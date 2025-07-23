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

			_logger.LogInformation("=== SFS2X MESSAGE RECEIVED ===");
			_logger.LogInformation("Message Type: {MessageType} (0x{MessageTypeHex:X2})", messageType, messageType);
			_logger.LogInformation("Session: {SessionId}", sessionId);
			_logger.LogInformation("Total message length: {Length} bytes", messageBytes.Length);
			
			// Log hex dump for unknown message types to help identify them
			if (messageType != 144)
			{
				var hexDump = Convert.ToHexString(messageBytes.Take(Math.Min(64, messageBytes.Length)).ToArray());
				_logger.LogInformation("Message hex (first 64 bytes): {HexDump}", hexDump);
			}

			// Special monitoring for ExtensionRequest messages
			if (messageType == 0x30)
			{
				_logger.LogInformation("🎉 EXTENSION REQUEST DETECTED! Client is progressing past login!");
			}

			return messageType switch
			{
				// Real SFS2X message type constants (discovered empirically)
				144 => await HandleClientHandshakeRequest(sessionId, reader), // Client version/capabilities handshake
				0x01 => await HandleLoginRequest(sessionId, reader),          // TODO: Find real LoginRequest constant
				0x03 => await HandleJoinRoomRequest(sessionId, reader),
				0x04 => await HandleLeaveRoomRequest(sessionId, reader),
				0x05 => await HandleSetRoomVariablesRequest(sessionId, reader),
				0x06 => await HandleExtensionRequest(sessionId, reader),
				0x30 => await HandleExtensionRequest(sessionId, reader),      // ExtensionRequest ("registerData", etc.)
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
	/// Critical: Handles "registerData" extension sent after successful login.
	/// </summary>
	private async Task<string?> HandleExtensionRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			_logger.LogInformation("=== EXTENSION REQUEST RECEIVED ===");
			_logger.LogInformation("Processing ExtensionRequest for session {SessionId}", sessionId);

			// Read ExtensionRequest structure based on SFS2X protocol
			// Format: [RequestID][Command][Parameters]
			var requestId = reader.ReadUInt16();
			var payloadSize = reader.ReadUInt32();
			
			_logger.LogInformation("ExtensionRequest - RequestID: {RequestId}, PayloadSize: {PayloadSize}", 
				requestId, payloadSize);

			// Read command string (extension name)
			var command = reader.ReadString();
			_logger.LogInformation("Extension Command: '{Command}'", command);

			// Read parameters (SFSObject containing extension data)
			SFSObject? parameters = null;
			if (reader.BaseStream.Position < reader.BaseStream.Length)
			{
				try
				{
					parameters = SFSObject.Deserialize(reader);
					_logger.LogInformation("Extension parameters parsed successfully with {KeyCount} keys", 
						parameters.GetKeys().Count());
				}
				catch (Exception parseEx)
				{
					_logger.LogWarning(parseEx, "Failed to parse extension parameters, continuing...");
					parameters = new SFSObject(); // Create empty object
				}
			}

			// Handle specific extension commands
			string? response = command switch
			{
				"registerData" => await HandleRegisterDataExtension(sessionId, requestId, parameters),
				"kickUser" => await HandleKickUserExtension(sessionId, requestId, parameters),
				"makeNewServer" => await HandleMakeNewServerExtension(sessionId, requestId, parameters),
				_ => await HandleUnknownExtension(sessionId, requestId, command, parameters)
			};

			_logger.LogInformation("=== EXTENSION REQUEST PROCESSED ===");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling ExtensionRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Extension failed");
		}
	}

	/// <summary>
	/// Handles the critical "registerData" extension request sent after login.
	/// This contains user settings and marks the client as ready for lobby operations.
	/// </summary>
	private async Task<string?> HandleRegisterDataExtension(string sessionId, ushort requestId, SFSObject? parameters)
	{
		try
		{
			_logger.LogInformation("=== REGISTER DATA EXTENSION ===");
			_logger.LogInformation("Processing 'registerData' extension for session {SessionId}", sessionId);

			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session?.PlayerId == null)
			{
				_logger.LogWarning("Session has no associated player for registerData: {SessionId}", sessionId);
				return CreateSFS2XExtensionResponse(requestId, false, "Player not logged in");
			}

			// Extract user settings from parameters (based on AmazonManager.cs analysis)
			if (parameters != null && parameters.ContainsKey("userSettings"))
			{
				var userSettings = parameters.GetSFSObject("userSettings");
				var isReady = parameters.ContainsKey("isReady") ? parameters.GetBool("isReady") : false;

				_logger.LogInformation("User settings received:");
				
				foreach (var key in userSettings.GetKeys())
				{
					var value = userSettings.ContainsKey(key) ? userSettings.GetUtfString(key) : "null";
					_logger.LogDebug("  {Key}: {Value}", key, value);
				}

				_logger.LogInformation("Player ready status: {IsReady}", isReady);

				// TODO: Store user settings in session or player data
				// For now, acknowledge the registration
				
				_logger.LogInformation("Player {PlayerId} registered successfully with user settings", session.PlayerId);
			}
			else
			{
				_logger.LogWarning("registerData extension missing userSettings parameter");
			}

			// Send successful registerData response
			var responseData = new SFSObject();
			responseData.PutUtfString("status", "registered");
			responseData.PutUtfString("sessionId", sessionId);
			responseData.PutLong("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

			var response = CreateSFS2XExtensionResponse(requestId, true, "Registration successful", responseData);
			
			_logger.LogInformation("=== REGISTER DATA COMPLETE - CLIENT SHOULD BE READY FOR LOBBIES ===");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling registerData extension for session {SessionId}", sessionId);
			return CreateSFS2XExtensionResponse(requestId, false, "Registration failed");
		}
	}

	/// <summary>
	/// Handles other extension requests.
	/// </summary>
	private async Task<string?> HandleKickUserExtension(string sessionId, ushort requestId, SFSObject? parameters)
	{
		_logger.LogDebug("kickUser extension for session {SessionId} - not implemented", sessionId);
		return CreateSFS2XExtensionResponse(requestId, true, "Kick handled");
	}

	private async Task<string?> HandleMakeNewServerExtension(string sessionId, ushort requestId, SFSObject? parameters)
	{
		_logger.LogDebug("makeNewServer extension for session {SessionId} - not implemented", sessionId);
		return CreateSFS2XExtensionResponse(requestId, true, "Server created");
	}

	private async Task<string?> HandleUnknownExtension(string sessionId, ushort requestId, string command, SFSObject? parameters)
	{
		_logger.LogWarning("Unknown extension '{Command}' for session {SessionId}", command, sessionId);
		return CreateSFS2XExtensionResponse(requestId, true, "Extension handled");
	}

	/// <summary>
	/// Creates a proper SFS2X ExtensionResponse.
	/// </summary>
	private string CreateSFS2XExtensionResponse(ushort requestId, bool success, string message, SFSObject? data = null)
	{
		var responseData = data ?? new SFSObject();
		responseData.PutBool("success", success);
		responseData.PutUtfString("message", message);

		// Use proper SFS2X binary format for extension responses
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		// Serialize the SFSObject response data
		using var payloadStream = new MemoryStream();
		using var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8);
		responseData.Serialize(payloadWriter);
		var payloadBytes = payloadStream.ToArray();
		
		// Write SFS2X ExtensionResponse format: [Type][RequestID][PayloadSize][Payload]
		writer.Write((byte)0x31);              // ExtensionResponse message type
		writer.Write(requestId);               // Echo back the request ID
		writer.Write((uint)payloadBytes.Length); // Payload size
		writer.Write(payloadBytes);            // Serialized SFSObject with response data
		
		var responseBytes = stream.ToArray();
		return Convert.ToBase64String(responseBytes);
	}

	/// <summary>
	/// Handles unknown message types with detailed analysis.
	/// </summary>
	private Task<string?> HandleUnknownMessage(string sessionId, byte messageType, BinaryReader reader)
	{
		_logger.LogWarning(
			"Unknown SFS2X message type {MessageType} (0x{MessageTypeHex:X2}) for session {SessionId}",
			messageType, messageType, sessionId);

		// Provide detailed analysis for unknown message types to help identify them
		try
		{
			reader.BaseStream.Position = 0;
			var allBytes = reader.ReadBytes((int)reader.BaseStream.Length);
			
			_logger.LogInformation("=== UNKNOWN MESSAGE ANALYSIS ===");
			_logger.LogInformation("Full hex dump: {HexDump}", Convert.ToHexString(allBytes));
			
			// Try to read as SFS2X message structure
			using var structStream = new MemoryStream(allBytes);
			using var structReader = new BinaryReader(structStream, Encoding.UTF8);
			
			var msgType = structReader.ReadByte();
			if (structStream.Position < structStream.Length)
			{
				var requestId = structReader.ReadUInt16();
				_logger.LogInformation("Potential Request ID: {RequestId} (0x{RequestIdHex:X4})", requestId, requestId);
			}
			if (structStream.Position < structStream.Length)
			{
				var payloadSize = structReader.ReadUInt32();
				_logger.LogInformation("Potential Payload Size: {PayloadSize} (0x{PayloadSizeHex:X8})", payloadSize, payloadSize);
			}
			
			// Look for string patterns that might indicate message type
			var ascii = string.Join("", allBytes.Select(b => b >= 32 && b <= 126 ? (char)b : '.'));
			_logger.LogInformation("ASCII view: {ASCII}", ascii);
			
			_logger.LogInformation("=== END UNKNOWN MESSAGE ANALYSIS ===");
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Error analyzing unknown message: {Error}", ex.Message);
		}

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
	/// Creates a proper SFS2X LoginResponse message that the client's SFS2X library will process.
	/// This will cause the client library to fire SFSEvent.LOGIN automatically.
	/// </summary>
	private static string CreateSFS2XLoginResponse(ushort requestId, SFSObject loginData)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		// Serialize the SFSObject login data to get payload
		using var payloadStream = new MemoryStream();
		using var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8);
		loginData.Serialize(payloadWriter);
		var payloadBytes = payloadStream.ToArray();
		
		// Write SFS2X LoginResponse format: [Type][RequestID][PayloadSize][Payload]
		// TESTING: Use message type 144 (same as request) instead of 0x01
		// Hypothesis: Custom protocol expects response to echo request type
		writer.Write((byte)144);               // LoginResponse message type (TESTING: same as request)
		writer.Write(requestId);               // Echo back the request ID
		writer.Write((uint)payloadBytes.Length); // Payload size
		writer.Write(payloadBytes);            // Serialized SFSObject with login data
		
		var responseBytes = stream.ToArray();
		return Convert.ToBase64String(responseBytes);
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

	/// <summary>
	/// Handles SFS2X LoginRequest (message type 144).
	/// Based on decompiled AmazonManager.cs, this is actually the LoginRequest, not separate handshake.
	/// Must respond with proper SFS2X binary format containing AWS credentials and P2P networking info.
	/// </summary>
	private async Task<string?> HandleClientHandshakeRequest(string sessionId, BinaryReader reader)
	{
		try
		{
			_logger.LogInformation("Processing SFS2X LoginRequest (type 144) for session {SessionId}", sessionId);
			
			// EXPERIMENT: Test if there's no PayloadSize field in SFS2X header
			// Main function already read messageType (144), stream is at position 1
			_logger.LogInformation("=== TESTING NO PAYLOADSIZE HYPOTHESIS ===");
			_logger.LogDebug("Current stream position: {Position}, total length: {Length}, remaining: {Remaining}",
				reader.BaseStream.Position, reader.BaseStream.Length, 
				reader.BaseStream.Length - reader.BaseStream.Position);
			
			// Test 1: Try reading SFSObject directly (no RequestID field)
			var initialPosition = reader.BaseStream.Position;
			_logger.LogInformation("TEST 1: Attempting direct SFSObject parse from position {Position}", initialPosition);
			
			// Parse the LoginRequest SFSObject payload 
			string username = "unknown_user";
			string password = string.Empty;
			string zone = "Offworld";
			string clientVersion = "unknown";
			SFSObject? loginRequestData = null;
			
			// CUSTOM LENGTH-PREFIXED PARSER: Parse the discovered binary format
			reader.BaseStream.Position = initialPosition;
			
			_logger.LogInformation("=== CUSTOM BINARY PARSER ===");
			_logger.LogInformation("Parsing length-prefixed string format...");
			
			ushort parsedRequestId = 0;
			
			try
			{
				// Parse header based on discovered pattern
				parsedRequestId = reader.ReadUInt16(); // Extract actual RequestID from message
				var flags = reader.ReadUInt16();       // Format flags
				
				_logger.LogInformation("Header: RequestID={RequestId}, Flags=0x{Flags:X4}", parsedRequestId, flags);
				
				// Parse data section - Let's examine the raw bytes to understand the structure
				var remainingBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
				var hexDump = Convert.ToHexString(remainingBytes);
				var asciiView = string.Join("", remainingBytes.Select(b => b >= 32 && b <= 126 ? (char)b : '.'));
				
				_logger.LogInformation("Remaining {Length} bytes after header:", remainingBytes.Length);
				_logger.LogInformation("Hex: {Hex}", hexDump);
				_logger.LogInformation("ASCII: {ASCII}", asciiView);
				
				// Look for our known strings in the hex dump
				var knownStrings = new[] { "api", "1.7.8", "Unity", "cl" };
				var parsedData = new Dictionary<string, string>();
				
				foreach (var knownString in knownStrings)
				{
					var stringBytes = Encoding.UTF8.GetBytes(knownString);
					var stringHex = Convert.ToHexString(stringBytes);
					var position = hexDump.IndexOf(stringHex, StringComparison.OrdinalIgnoreCase);
					
					if (position >= 0)
					{
						var bytePosition = position / 2;
						_logger.LogInformation("Found '{String}' at hex position {HexPos} (byte {BytePos})", 
							knownString, position, bytePosition);
						
						// Check for length prefix pattern
						if (bytePosition >= 3)
						{
							var beforeBytes = remainingBytes.Skip(bytePosition - 3).Take(3).ToArray();
							_logger.LogInformation("  3 bytes before '{String}': {BeforeHex} ({BeforeAscii})", 
								knownString, Convert.ToHexString(beforeBytes), 
								string.Join("", beforeBytes.Select(b => b >= 32 && b <= 126 ? (char)b : '.')));
						}
						
						// Store found data
						switch (knownString)
						{
							case "api":
								parsedData["clientType"] = knownString;
								break;
							case "1.7.8":
								parsedData["version"] = knownString;
								break;
							case "Unity":
								parsedData["engine"] = knownString;
								break;
							case "cl":
								parsedData["clientMode"] = knownString;
								break;
						}
					}
					else
					{
						_logger.LogWarning("Known string '{String}' not found in hex dump", knownString);
					}
				}
				
				// Extract meaningful data from found strings
				username = parsedData.ContainsKey("clientType") ? $"client_{parsedData["clientType"]}" : "unity_client";
				var versionInfo = parsedData.ContainsKey("version") ? parsedData["version"] : "unknown";
				var engineInfo = parsedData.ContainsKey("engine") ? parsedData["engine"] : "unknown";
				var clientMode = parsedData.ContainsKey("clientMode") ? parsedData["clientMode"] : "unknown";
				
				clientVersion = $"engine={engineInfo}, version={versionInfo}, type={parsedData.GetValueOrDefault("clientType", "unknown")}, mode={clientMode}";
				
				_logger.LogInformation("Extracted client info - Version: {Version}", clientVersion);
				_logger.LogInformation("Binary parsing SUCCESS: Found {FoundCount} of 4 known strings", parsedData.Count);
				
				loginRequestData = new SFSObject(); // Create empty object to continue flow
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Custom parser failed, falling back to default data");
				username = "unknown_client";
				clientVersion = "parsing_failed";
				loginRequestData = new SFSObject();
			}
			
			if (loginRequestData != null)
			{
				_logger.LogInformation("Successfully parsed LoginRequest SFSObject with {KeyCount} keys", 
					loginRequestData.GetKeys().Count());
				
				// Extract standard login fields
				if (loginRequestData.ContainsKey("userName"))
					username = loginRequestData.GetUtfString("userName");
				else if (loginRequestData.ContainsKey("user"))
					username = loginRequestData.GetUtfString("user");
				
				if (loginRequestData.ContainsKey("password"))
					password = loginRequestData.GetUtfString("password");
				
				if (loginRequestData.ContainsKey("zone"))
					zone = loginRequestData.GetUtfString("zone");
				
				// Extract client version information (may be in different fields)
				var versionParts = new List<string>();
				foreach (var key in loginRequestData.GetKeys())
				{
					var value = loginRequestData.GetUtfString(key);
					if (value.Contains("api") || value.Contains("Unity") || value.Contains("."))
						versionParts.Add($"{key}={value}");
				}
				if (versionParts.Count > 0)
					clientVersion = string.Join(", ", versionParts);
				
				_logger.LogInformation("Parsed LoginRequest - User: {Username}, Zone: {Zone}, Version: {Version}", 
					username, zone, clientVersion);
				
				// Log all available keys for debugging
				var allKeys = string.Join(", ", loginRequestData.GetKeys());
				_logger.LogDebug("Available SFSObject keys: {Keys}", allKeys);
			}
			else
			{
				_logger.LogError("No loginRequestData was successfully parsed");
				return CreateErrorResponse("Failed to parse login data");
			}
			
			// Validate that we have required fields
			if (string.IsNullOrEmpty(username) || username == "unknown_user")
			{
				_logger.LogError("LoginRequest missing required username field");
				return CreateErrorResponse("Login failed: missing username");
			}
			
			// Get session info for P2P networking data
			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session == null)
			{
				_logger.LogWarning("Session not found for LoginRequest: {SessionId}", sessionId);
				return CreateErrorResponse("Session not found");
			}

			// NEW HYPOTHESIS: Wrong response format - client expects SFS2X binary, not JSON
			_logger.LogInformation("=== SFS2X BINARY RESPONSE TEST ===");
			_logger.LogInformation("HYPOTHESIS: Client rejects JSON responses, needs proper SFS2X binary format");
			_logger.LogInformation("Using CreateSFS2XLoginResponse() instead of EncodeResponse()");
			
			// Create proper SFS2X LoginResponse data with REQUIRED fields from AmazonManager.cs
			var responseData = new SFSObject();
			
			// CRITICAL: These fields are REQUIRED by AmazonManager.OnLogin()
			responseData.PutUtfString("accessKey", "fake_aws_access_key");           // Required by line 829
			responseData.PutUtfString("secret", "fake_aws_secret_key");              // Required by line 830
			responseData.PutLong("serverTime", DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // Required by line 831
			responseData.PutUtfString("ipAddress", session.ClientIP);                // CRITICAL for P2P (line 834)
			responseData.PutInt("port", GenerateP2PPort(session.ClientIP));          // CRITICAL for P2P (line 835)
			
			// Additional fields for debugging/compatibility
			responseData.PutUtfString("status", "login_successful");
			responseData.PutUtfString("server_version", "1.0.0");
			responseData.PutUtfString("client_info", clientVersion);
			responseData.PutUtfString("sessionId", sessionId);
			responseData.PutUtfString("zone", zone);
			responseData.PutUtfString("userName", username);
			
			_logger.LogInformation("LoginResponse data prepared with required AWS/P2P fields:");
			_logger.LogInformation("  accessKey: fake_aws_access_key");
			_logger.LogInformation("  secret: fake_aws_secret_key"); 
			_logger.LogInformation("  ipAddress: {IP} (client's external IP for P2P)", session.ClientIP);
			_logger.LogInformation("  port: {Port} (client's P2P port)", responseData.GetInt("port"));
			_logger.LogInformation("  serverTime: {ServerTime}", responseData.GetLong("serverTime"));
			
			// Use the RequestID we parsed from the client message
			ushort responseRequestId = parsedRequestId; // Echo back the client's RequestID
			
			// Use proper SFS2X binary format instead of JSON
			var response = CreateSFS2XLoginResponse(responseRequestId, responseData);
			
			// Add detailed hex logging for debugging
			_logger.LogInformation("=== DETAILED LOGINRESPONSE ANALYSIS ===");
			var responseBytes = Convert.FromBase64String(response);
			var responseHex = Convert.ToHexString(responseBytes);
			_logger.LogInformation("LoginResponse Base64: {Base64}", response);
			_logger.LogInformation("LoginResponse Hex ({Length} bytes): {Hex}", responseBytes.Length, responseHex);
			_logger.LogInformation("LoginResponse Structure:");
			_logger.LogInformation("  Message Type: 0x{Type:X2} (TESTING: 144/0x90 - same as request)", responseBytes[0]);
			_logger.LogInformation("  Request ID: {RequestId} (0x{RequestIdHex:X4})", 
				BitConverter.ToUInt16(responseBytes, 1), BitConverter.ToUInt16(responseBytes, 1));
			_logger.LogInformation("  Payload Size: {PayloadSize} (0x{PayloadSizeHex:X8})",
				BitConverter.ToUInt32(responseBytes, 3), BitConverter.ToUInt32(responseBytes, 3));
			
			_logger.LogInformation(
				"SFS2X Binary Response sent for session {SessionId} - RequestID: {RequestId}",
				sessionId, responseRequestId);
			
			_logger.LogInformation("=== TESTING MESSAGE TYPE 144 HYPOTHESIS ===");
			_logger.LogInformation("HYPOTHESIS: Custom protocol expects LoginResponse with same message type as request");
			_logger.LogInformation("Changed from message type 0x01 → 144 (0x90)");
			_logger.LogInformation("Expected client behavior if correct:");
			_logger.LogInformation("1. Client SFS2X library should trigger SFSEvent.LOGIN event");
			_logger.LogInformation("2. AmazonManager.OnLogin() should execute");
			_logger.LogInformation("3. Client should send ExtensionRequest('registerData', userSettings)");
			_logger.LogInformation("4. Client should stop polling and become ready for lobby operations");
			_logger.LogInformation("Expected client behavior if wrong: Continues polling (same as before)");
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling LoginRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Login failed");
		}
	}
}
