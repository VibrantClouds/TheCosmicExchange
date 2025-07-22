using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Enums;
using OffworldLobbyServer.Shared.Models;
using OffworldLobbyServer.Shared.SFS2X;

namespace OffworldLobbyServer.Core.Services;

/// <summary>
/// Processes SFS2X protocol messages from BlueBox data commands
/// Handles LoginRequest, CreateRoomRequest, JoinRoomRequest, etc.
/// </summary>
public class SFS2XMessageProcessor
{
	private readonly ISessionManager _sessionManager;
	private readonly IRoomManager _roomManager;
	private readonly ILogger<SFS2XMessageProcessor> _logger;

	public SFS2XMessageProcessor(
		ISessionManager sessionManager,
		IRoomManager roomManager,
		ILogger<SFS2XMessageProcessor> logger)
	{
		_sessionManager = sessionManager;
		_roomManager = roomManager;
		_logger = logger;
	}

	/// <summary>
	/// Processes base64-encoded SFS2X message data.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="base64Data">Base64-encoded message.</param>
	/// <returns>Base64-encoded response or null for acknowledgment.</returns>
	public async Task<string?> ProcessSFS2XMessage(string sessionId, string base64Data)
	{
		try
		{
			// Decode base64 data
			var messageBytes = Convert.FromBase64String(base64Data);

			_logger.LogDebug(
				"Processing SFS2X message for session {SessionId}: {Length} bytes",
				sessionId, messageBytes.Length);

			using var stream = new MemoryStream(messageBytes);
			using var reader = new BinaryReader(stream, Encoding.UTF8);

			// Read message type (first few bytes typically indicate message type)
			var messageType = reader.ReadByte();

			_logger.LogDebug("SFS2X message type: {MessageType}", messageType);

			return messageType switch
			{
				// These are typical SFS2X message type identifiers
				0x01 => await HandleLoginRequest(sessionId, reader),
				0x02 => await HandleCreateRoomRequest(sessionId, reader),
				0x03 => await HandleJoinRoomRequest(sessionId, reader),
				0x04 => await HandleLeaveRoomRequest(sessionId, reader),
				0x05 => await HandleSetRoomVariablesRequest(sessionId, reader),
				0x06 => await HandleExtensionRequest(sessionId, reader),
				_ => await HandleUnknownMessage(sessionId, messageType, reader)
			};
		}
		catch (FormatException ex)
		{
			_logger.LogWarning(ex, "Invalid base64 data in session {SessionId}", sessionId);
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing SFS2X message for session {SessionId}", sessionId);
			return null;
		}
	}

	/// <summary>
	/// Handles SFS2X LoginRequest - critical for providing IP/port info.
	/// </summary>
	private async Task<string> HandleLoginRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			_logger.LogInformation("Processing LoginRequest for session {SessionId}", sessionId);

			// Parse basic login request fields
			var userName = reader.ReadString();    // PlayerID.ToString()
			var password = reader.ReadString();    // Usually empty
			var zone = reader.ReadString();        // "Offworld"

			_logger.LogDebug("LoginRequest - User: {UserName}, Zone: {Zone}", userName, zone);

			// Get session info to extract client IP
			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session == null)
			{
				_logger.LogWarning("Session not found for LoginRequest: {SessionId}", sessionId);
				return CreateErrorResponse("Session not found");
			}

			// Create login response with P2P networking info (CRITICAL)
			var loginResponse = new
			{
				success = true,
				data = new
				{
					accessKey = "fake_aws_access_key",           // Dummy AWS credentials
					secret = "fake_aws_secret_key",              // Game checks these but doesn't use for P2P
					serverTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
					ipAddress = session.ClientIP,                // CRITICAL: Client's external IP
					port = GenerateP2PPort(session.ClientIP) // CRITICAL: Client's P2P port
				},
			};

			// Update session with player info
			var playerId = ParsePlayerIDFromString(userName);
			await _sessionManager.SetSessionPlayerAsync(sessionId, playerId);

			_logger.LogInformation(
				"LoginResponse sent for {UserName} - IP: {IP}, Port: {Port}",
				userName, session.ClientIP, loginResponse.data.port);

			return EncodeResponse(loginResponse);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling LoginRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Login failed");
		}
	}

	/// <summary>
	/// Handles SFS2X CreateRoomRequest - parses 21-element lobby settings.
	/// </summary>
	private async Task<string> HandleCreateRoomRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			_logger.LogInformation("Processing CreateRoomRequest for session {SessionId}", sessionId);

			// Parse room settings structure
			var roomName = reader.ReadString();
			var maxUsers = reader.ReadInt16();
			var groupId = reader.ReadString();      // Should be "lobbies"
			var password = reader.ReadString();

			_logger.LogDebug(
				"CreateRoomRequest - Name: {RoomName}, MaxUsers: {MaxUsers}, Group: {GroupId}",
				roomName, maxUsers, groupId);

			// Parse room variables - looking for "lobbySettings" variable
			var variableCount = reader.ReadInt32();
			SFSArray? lobbySettingsArray = null;

			for (int i = 0; i < variableCount; i++)
			{
				var varName = reader.ReadString();
				var varType = reader.ReadByte();

				if (varName == "lobbySettings" && varType == 7) // SFSArray type
				{
					lobbySettingsArray = SFSArray.Deserialize(reader);
					_logger.LogDebug("Found lobbySettings array with {Size} elements", lobbySettingsArray.Size);
				}
				else
				{
					// Skip other variables
					SkipVariableData(reader, varType);
				}
			}

			if (lobbySettingsArray == null)
			{
				_logger.LogWarning("CreateRoomRequest missing lobbySettings array");
				return CreateErrorResponse("Invalid lobby settings");
			}

			// Convert 21-element SFS2X array to LobbySettings
			var lobbySettings = ParseLobbySettingsFromSFSArray(lobbySettingsArray);
			lobbySettings.Name = roomName;
			lobbySettings.MaxPlayers = maxUsers;
			lobbySettings.Password = password;

			// Get session player for ownership
			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session?.PlayerId == null)
			{
				_logger.LogWarning("Session has no associated player for CreateRoomRequest: {SessionId}", sessionId);
				return CreateErrorResponse("Player not logged in");
			}

			// Create the room
			var room = await _roomManager.CreateRoomAsync(lobbySettings, session.PlayerId, password);

			// Associate session with room
			await _sessionManager.SetSessionRoomAsync(sessionId, room.RoomId);

			_logger.LogInformation(
				"Room created successfully: {RoomName} (ID: {RoomId}) by {Owner}",
				roomName, room.RoomId, session.PlayerId);

			// Create room creation success response
			var response = new
			{
				success = true,
				room = new
				{
					id = room.RoomId,
					name = room.Name,
					userCount = room.UserCount,
					maxUsers = room.MaxUsers,
					variables = new { lobbySettings = "configured" },
				},
				user = new
				{
					name = session.PlayerId.ToString(),
					id = room.RoomId, // User ID in room context
					variables = new { }
				},
			};

			return EncodeResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling CreateRoomRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Room creation failed");
		}
	}

	/// <summary>
	/// Handles SFS2X JoinRoomRequest.
	/// </summary>
	private async Task<string> HandleJoinRoomRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			_logger.LogInformation("Processing JoinRoomRequest for session {SessionId}", sessionId);

			var roomId = reader.ReadInt32();
			var password = reader.ReadString();

			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session?.PlayerId == null)
			{
				return CreateErrorResponse("Player not logged in");
			}

			var success = await _roomManager.JoinRoomAsync(roomId, session.PlayerId, password);
			if (!success)
			{
				return CreateErrorResponse("Failed to join room");
			}

			await _sessionManager.SetSessionRoomAsync(sessionId, roomId);

			var room = await _roomManager.GetRoomAsync(roomId);
			var response = new
			{
				success = true,
				room = new
				{
					id = room!.RoomId,
					name = room.Name,
					userCount = room.UserCount,
					maxUsers = room.MaxUsers
				},
			};

			_logger.LogInformation("Player {PlayerId} joined room {RoomId}", session.PlayerId, roomId);
			return EncodeResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling JoinRoomRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Join room failed");
		}
	}

	/// <summary>
	/// Handles leaving a room.
	/// </summary>
	private async Task<string?> HandleLeaveRoomRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session?.PlayerId != null && session.CurrentRoomId.HasValue)
			{
				await _roomManager.LeaveRoomAsync(session.CurrentRoomId.Value, session.PlayerId);
				await _sessionManager.SetSessionRoomAsync(sessionId, 0); // Clear room association

				_logger.LogInformation("Player {PlayerId} left room {RoomId}", session.PlayerId, session.CurrentRoomId.Value);
			}

			return EncodeResponse(new { success = true });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling LeaveRoomRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Leave room failed");
		}
	}

	/// <summary>
	/// Handles room variable updates (lobby settings changes).
	/// </summary>
	private async Task<string?> HandleSetRoomVariablesRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session?.PlayerId == null || !session.CurrentRoomId.HasValue)
			{
				return CreateErrorResponse("Not in a room");
			}

			// TODO: Parse variable updates and apply to room
			_logger.LogDebug(
				"Room variable update for session {SessionId} in room {RoomId}",
				sessionId, session.CurrentRoomId.Value);

			return EncodeResponse(new { success = true });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling SetRoomVariablesRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Variable update failed");
		}
	}

	/// <summary>
	/// Handles extension requests (custom game protocol).
	/// </summary>
	private Task<string?> HandleExtensionRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			// Extension requests are typically game-specific
			_logger.LogDebug("Extension request for session {SessionId}", sessionId);
			return Task.FromResult<string?>(EncodeResponse(new { success = true, data = "extension_handled" }));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling ExtensionRequest for session {SessionId}", sessionId);
			return Task.FromResult<string?>(CreateErrorResponse("Extension failed"));
		}
	}

	/// <summary>
	/// Handles unknown message types.
	/// </summary>
	private Task<string?> HandleUnknownMessage(string sessionId, byte messageType, BinaryReader reader)
	{
		_logger.LogWarning(
			"Unknown SFS2X message type {MessageType} for session {SessionId}",
			messageType, sessionId);
		return Task.FromResult<string?>(null); // Acknowledge unknown message
	}

	/// <summary>
	/// Parses 21-element SFS2X array into LobbySettings object.
	/// </summary>
	private LobbySettings ParseLobbySettingsFromSFSArray(SFSArray array)
	{
		if (array.Size < 21)
		{
			throw new ArgumentException($"Invalid lobby settings array size: {array.Size}, expected 21 elements");
		}

		var settings = new LobbySettings
		{
			// Index 0: Lobby name
			Name = array.GetUtfString(0),

			// Index 1: Lobby type
			KindOfLobby = (LobbyType)array.GetByte(1),

			// Index 2: Steam version key
			SteamVersionKey = array.GetUtfString(2),

			// Index 3: Game setup
			GameSetup = (GameSetupType)array.GetShort(3),

			// Index 4: Rules set
			RulesSet = (RulesSetType)array.GetShort(4),

			// Index 5: Want replay file
			WantReplayFile = array.GetBool(5),

			// Index 6: Location
			Location = (LocationType)array.GetShort(6),

			// Index 7: Invalid human HQ
			InvalidHumanHQ = array.GetBoolArray(7),

			// Index 8: Want AI players
			WantAIplayers = array.GetBool(8),

			// Index 9: Map size
			MapSizeIndex = (MapSizeType)array.GetByte(9),

			// Index 10: Terrain class
			TerrainClassIndex = (TerrainClassType)array.GetShort(10),

			// Index 11: Game speed
			GameSpeedIndex = (GameSpeedType)array.GetByte(11),

			// Index 12: Map name
			MapName = array.GetUtfString(12),

			// Index 13: Seed
			Seed = array.GetInt(13),

			// Index 14: Latitude
			Latitude = (LatitudeType)array.GetShort(14),

			// Index 15: Resource minimum
			ResourceMinimum = (ResourceMinimumType)array.GetByte(15),

			// Index 16: Resource presence
			ResourcePresence = (ResourcePresenceType)array.GetByte(16),

			// Index 17: Colony class
			ColonyClass = (ColonyClassType)array.GetShort(17),

			// Index 18: Game options
			GameOptions = array.GetBoolArray(18),
		};

		// Index 19: Team assignments
		if (array.Size > 19)
		{
			var teamObject = array.GetSFSObject(19);
			foreach (var key in teamObject.GetKeys())
			{
				var playerId = ParsePlayerIDFromString(key);
				var teamNumber = teamObject.GetShort(key);
				settings.TeamNumbers[playerId.PlayerID] = teamNumber;
			}
		}

		// Index 20: Handicap assignments
		if (array.Size > 20)
		{
			var handicapObject = array.GetSFSObject(20);
			foreach (var key in handicapObject.GetKeys())
			{
				var playerId = ParsePlayerIDFromString(key);
				var handicapValue = handicapObject.GetShort(key);
				settings.Handicaps[playerId.PlayerID] = handicapValue;
			}
		}

		return settings;
	}

	/// <summary>
	/// Generates a P2P port for the client (critical for Unity P2P).
	/// </summary>
	private static int GenerateP2PPort(string clientIP)
	{
		// Generate a consistent port based on client IP
		// In production, you might want to use a port pool or registry
		var hash = clientIP.GetHashCode();
		return 50000 + Math.Abs(hash % 10000); // Port range: 50000-59999
	}

	/// <summary>
	/// Parses PlayerID from string representation.
	/// </summary>
	private static CombinedID ParsePlayerIDFromString(string playerIdString)
	{
		// TODO: Implement proper PlayerID parsing based on game's format
		// For now, create a simple CombinedID
		return new CombinedID(playerIdString, StorefrontId.Steam, string.Empty, 0);
	}

	/// <summary>
	/// Encodes response object as base64 JSON.
	/// </summary>
	private static string EncodeResponse(object response)
	{
		var json = JsonSerializer.Serialize(response);
		var bytes = Encoding.UTF8.GetBytes(json);
		return Convert.ToBase64String(bytes);
	}

	/// <summary>
	/// Creates an error response.
	/// </summary>
	private string CreateErrorResponse(string errorMessage)
	{
		var error = new
		{
			success = false,
			error = errorMessage,
		};
		return EncodeResponse(error);
	}

	/// <summary>
	/// Skips variable data based on type.
	/// </summary>
	private static void SkipVariableData(BinaryReader reader, byte varType)
	{
		switch (varType)
		{
			case 1: // String
				reader.ReadString();
				break;
			case 2: // Byte
				reader.ReadByte();
				break;
			case 3: // Short
				reader.ReadInt16();
				break;
			case 4: // Int
				reader.ReadInt32();
				break;
			case 5: // Bool
				reader.ReadBoolean();
				break;
			case 6: // Bool array
				var length = reader.ReadInt32();
				for (int i = 0; i < length; i++)
				{
					reader.ReadBoolean();
				}

				break;
			case 7: // SFSObject or SFSArray
				SFSArray.Deserialize(reader); // This handles both
				break;
		}
	}
}
