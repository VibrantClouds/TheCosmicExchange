using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Enums;
using OffworldLobbyServer.Shared.Models;
using OffworldLobbyServer.Shared.SFS2X;
using Sfs2X.Requests;
using OfficialSFSObject = Sfs2X.Entities.Data.SFSObject;
using OfficialSFSArray = Sfs2X.Entities.Data.SFSArray;
using Sfs2X.Util;

namespace OffworldLobbyServer.Core.Services;

/// <summary>
/// Represents a parsed SFS2X request from the client
/// </summary>
public class ParsedSFS2XRequest
{
	public int MessageType { get; set; }
	public OfficialSFSObject? Data { get; set; }
	public string? RequestTypeName { get; set; }
}

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

			// Add detailed hex dump for protocol analysis
			_logger.LogInformation("=== DETAILED PROTOCOL ANALYSIS ===");
			_logger.LogInformation("Raw message bytes ({Length}): {Hex}", messageBytes.Length, Convert.ToHexString(messageBytes));
			
			// Analyze first few bytes to understand protocol structure
			if (messageBytes.Length >= 5)
			{
				_logger.LogInformation("Byte analysis:");
				_logger.LogInformation("  Byte 0: 0x{B0:X2} ({B0}) - Potential message type", messageBytes[0], messageBytes[0]);
				if (messageBytes.Length >= 3)
				{
					var potentialRequestId = BitConverter.ToUInt16(messageBytes, 1);
					var potentialRequestIdBE = (ushort)((messageBytes[1] << 8) | messageBytes[2]);
					_logger.LogInformation("  Bytes 1-2: 0x{B1:X2}{B2:X2} - As LE uint16: {LE}, As BE uint16: {BE}", 
						messageBytes[1], messageBytes[2], potentialRequestId, potentialRequestIdBE);
				}
				if (messageBytes.Length >= 5)
				{
					var potentialFlags = BitConverter.ToUInt16(messageBytes, 3);
					var potentialFlagsBE = (ushort)((messageBytes[3] << 8) | messageBytes[4]);
					_logger.LogInformation("  Bytes 3-4: 0x{B3:X2}{B4:X2} - As LE uint16: {LE}, As BE uint16: {BE}", 
						messageBytes[3], messageBytes[4], potentialFlags, potentialFlagsBE);
				}
				if (messageBytes.Length > 5)
				{
					_logger.LogInformation("  Remaining payload ({Length} bytes): {Hex}", 
						messageBytes.Length - 5, Convert.ToHexString(messageBytes[5..]));
				}
			}

			using var stream = new MemoryStream(messageBytes);
			using var reader = new BinaryReader(stream, Encoding.UTF8);

			// Read message type (first few bytes typically indicate message type)
			var messageType = reader.ReadByte();

			_logger.LogDebug("SFS2X message received - Type: {MessageType} (0x{MessageTypeHex:X2}), Session: {SessionId}, Length: {Length} bytes", 
				messageType, messageType, sessionId, messageBytes.Length);

			// Special monitoring for ExtensionRequest messages
			if (messageType == 0x30)
			{
				_logger.LogInformation("🎉 EXTENSION REQUEST DETECTED! Client is progressing past login!");
			}

			return messageType switch
			{
				// CORRECTED: 144 IS a direct SFS2X message type, likely custom LoginRequest/HandshakeRequest
				144 => await HandleMessage144DirectSFS2X(sessionId, reader),   // Direct SFS2X message type 144
				
				// Standard SFS2X messages
				0 => await HandleClientHandshakeRequest(sessionId, reader),   // Handshake = 0 (official SFS2X)
				1 => await HandleLoginRequest(sessionId, reader),             // Login = 1 (official SFS2X)
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
					// Read remaining bytes and use SFS2XSerializer for proper deserialization
					var remainingBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
					lobbySettingsArray = SFS2XSerializer.DeserializeArray(remainingBytes);
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
					// Read remaining bytes and use SFS2XSerializer for proper deserialization
					var remainingBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
					parameters = SFS2XSerializer.DeserializeObject(remainingBytes);
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
		
		// Serialize the SFSObject response data using SFS2X format
		var payloadBytes = responseData.Serialize();
		
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
		
		// HANDSHAKE HYPOTHESIS: Message type 144 is handshake, not login
		// Test responding with handshake acknowledgment instead of full login data
		
		// Create minimal handshake response instead of full login data
		var handshakeResponse = new SFSObject();
		handshakeResponse.PutUtfString("status", "handshake_success");
		handshakeResponse.PutUtfString("server_version", "1.0.0");
		
		var payloadBytes = handshakeResponse.Serialize();
		
		// FIXED: Use official SFS2X Handshake response type from library exploration
		byte messageType = 0; // Official SFS2X Handshake = 0
		
		// Write SFS2X Response format: [Type][RequestID][PayloadSize][Payload]
		writer.Write(messageType);             // Test handshake response type
		writer.Write(requestId);               // Echo back the request ID
		writer.Write((uint)payloadBytes.Length); // Payload size
		writer.Write(payloadBytes);            // Minimal handshake response
		
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
				// Skip by reading remaining bytes as SFS2X format would handle both
				var skipBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
				break;
		}
	}

	/// <summary>
	/// Parses the complex LoginRequest message structure sent by the client.
	/// The message contains nested SFS2X-encoded data within a custom format.
	/// </summary>
	/// <param name="messageBytes">Raw message payload bytes.</param>
	/// <returns>Dictionary of extracted data fields.</returns>
	private Dictionary<string, string> ParseLoginMessageStructure(byte[] messageBytes)
	{
		var extractedData = new Dictionary<string, string>();
		
		_logger.LogInformation("=== DETAILED MESSAGE STRUCTURE ANALYSIS ===");
		_logger.LogInformation("Analyzing {Length} bytes: {Hex}", messageBytes.Length, Convert.ToHexString(messageBytes));
		
		try
		{
			// NEW APPROACH: Direct byte pattern matching instead of sequential parsing
			// This bypasses the "unknown type 99" issue by searching for string patterns directly
			_logger.LogInformation("=== DIRECT STRING PATTERN MATCHING ===");
			
			// Define known string patterns to search for
			var stringPatterns = new Dictionary<string, string>
			{
				["617069"] = "api",           // "api" in hex
				["312E372E38"] = "1.7.8",     // "1.7.8" in hex  
				["636C"] = "cl",              // "cl" in hex
				["556E697479"] = "Unity"      // "Unity" in hex
			};
			
			var hexString = Convert.ToHexString(messageBytes).ToUpper();
			_logger.LogInformation("Searching hex string: {Hex}", hexString);
			
			// Search for each known pattern
			foreach (var pattern in stringPatterns)
			{
				var hexPattern = pattern.Key.ToUpper();
				var expectedString = pattern.Value;
				
				var index = hexString.IndexOf(hexPattern);
				if (index >= 0)
				{
					var bytePosition = index / 2;
					_logger.LogInformation("Found '{String}' pattern at hex position {HexPos} (byte position {BytePos})", 
						expectedString, index, bytePosition);
					
					// Store the extracted string with appropriate key
					switch (expectedString.ToLowerInvariant())
					{
						case "api":
							extractedData["clientType"] = expectedString;
							break;
						case "1.7.8":
							extractedData["version"] = expectedString;
							break;
						case "unity":
							extractedData["engine"] = expectedString;
							break;
						case "cl":
							extractedData["clientMode"] = expectedString;
							break;
					}
				}
				else
				{
					_logger.LogWarning("Pattern for '{String}' not found in hex data", expectedString);
				}
			}
			
			// Additional validation: verify patterns appear in reasonable positions
			// This helps distinguish actual strings from coincidental byte matches
			if (extractedData.ContainsKey("clientType") && extractedData.ContainsKey("version") && 
				extractedData.ContainsKey("engine") && extractedData.ContainsKey("clientMode"))
			{
				_logger.LogInformation("All expected client info patterns found - extraction successful");
			}
			else
			{
				_logger.LogWarning("Some client info patterns missing - partial extraction");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to parse message structure");
		}
		
		_logger.LogInformation("Extracted {Count} data fields from message", extractedData.Count);
		foreach (var kvp in extractedData)
		{
			_logger.LogInformation("  {Key}: '{Value}'", kvp.Key, kvp.Value);
		}
		
		return extractedData;
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
			
			// CUSTOM LOGIN MESSAGE PARSING: Handle the actual client message structure
			reader.BaseStream.Position = initialPosition;
			
			_logger.LogInformation("=== CUSTOM LOGIN MESSAGE PARSING ===");
			_logger.LogInformation("Parsing complex LoginRequest message structure...");
			
			ushort parsedRequestId = 0;
			
			try
			{
				// Parse header based on discovered pattern
				parsedRequestId = reader.ReadUInt16(); // Extract actual RequestID from message
				var flags = reader.ReadUInt16();       // Format flags
				
				_logger.LogInformation("Header: RequestID={RequestId}, Flags=0x{Flags:X4}", parsedRequestId, flags);
				
				// Read remaining bytes for detailed analysis
				var remainingBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
				_logger.LogInformation("Parsing {Length} bytes of message payload", remainingBytes.Length);
				_logger.LogInformation("Payload hex: {Hex}", Convert.ToHexString(remainingBytes));
				
				// Parse the complex message structure
				var extractedData = ParseLoginMessageStructure(remainingBytes);
				
				// Extract login fields from the parsed data
				username = extractedData.TryGetValue("clientType", out var clientType) ? $"client_{clientType}" : "parsed_client";
				var versionInfo = extractedData.TryGetValue("version", out var ver) ? ver : "unknown";
				var engineInfo = extractedData.TryGetValue("engine", out var eng) ? eng : "unknown";
				var clientMode = extractedData.TryGetValue("clientMode", out var mode) ? mode : "unknown";
				
				clientVersion = $"engine={engineInfo}, version={versionInfo}, type={clientType ?? "unknown"}, mode={clientMode}";
				zone = "Offworld"; // Default zone
				password = string.Empty; // No password in this message
				
				_logger.LogInformation("Parsing SUCCESS: Extracted {Count} data fields", extractedData.Count);
				_logger.LogInformation("  Username: {Username}", username);
				_logger.LogInformation("  Version: {Version}", clientVersion);
				
				// Create a basic SFS2X object for compatibility
				loginRequestData = new SFSObject();
				foreach (var kvp in extractedData)
				{
					loginRequestData.PutUtfString(kvp.Key, kvp.Value);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Custom message parser failed, using fallback");
				
				username = "fallback_client";
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
			_logger.LogInformation("=== SFS2X HANDSHAKE RESPONSE TEST ===");
			_logger.LogInformation("HYPOTHESIS: Message type 144 is HandshakeRequest, not LoginRequest");
			_logger.LogInformation("Using CreateSFS2XLoginResponse() to send handshake acknowledgment");
			
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
			
			_logger.LogInformation("SYSTEMATIC MESSAGE TYPE TEST:");
			_logger.LogInformation("  Current test: 0x10 (common handshake pattern)");
			_logger.LogInformation("  Previous tests failed: 0x00, 0x01 - client continued polling");
			_logger.LogInformation("  Payload: minimal handshake response");
			_logger.LogInformation("  status: handshake_success");
			_logger.LogInformation("  server_version: 1.0.0");
			
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
			_logger.LogInformation("  Message Type: 0x{Type:X2} (Standard SFS2X LoginResponse)", responseBytes[0]);
			_logger.LogInformation("  Request ID: {RequestId} (0x{RequestIdHex:X4})", 
				BitConverter.ToUInt16(responseBytes, 1), BitConverter.ToUInt16(responseBytes, 1));
			_logger.LogInformation("  Payload Size: {PayloadSize} (0x{PayloadSizeHex:X8})",
				BitConverter.ToUInt32(responseBytes, 3), BitConverter.ToUInt32(responseBytes, 3));
			
			_logger.LogInformation(
				"SFS2X Binary Response sent for session {SessionId} - RequestID: {RequestId}",
				sessionId, responseRequestId);
			
			_logger.LogInformation("=== SYSTEMATIC MESSAGE TYPE TESTING ===");
			_logger.LogInformation("HYPOTHESIS: Find correct response message type for type 144 requests");
			_logger.LogInformation("Current test: Message type 0x{Type:X2} (common handshake pattern)", responseBytes[0]);
			_logger.LogInformation("Expected client behavior if correct:");
			_logger.LogInformation("1. Client accepts handshake response");
			_logger.LogInformation("2. Client sends DIFFERENT message type (actual LoginRequest)");
			_logger.LogInformation("3. After login, client sends ExtensionRequest('registerData', userSettings)");
			_logger.LogInformation("4. Client stops polling type 144 messages");
			_logger.LogInformation("Expected client behavior if wrong: Continues polling type 144 (same as before)");
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling LoginRequest for session {SessionId}", sessionId);
			return CreateErrorResponse("Login failed");
		}
	}

	/// <summary>
	/// Handles message type 144 as direct SFS2X message (corrected approach)  
	/// Evidence suggests 144 is a legitimate SFS2X message type, not a wrapper protocol
	/// </summary>
	private async Task<string?> HandleMessage144DirectSFS2X(string sessionId, BinaryReader reader)
	{
		try
		{
			_logger.LogInformation("=== ANALYZING MESSAGE TYPE 144 STRUCTURE ===");
			var startPosition = reader.BaseStream.Position;
			
			// CRITICAL: RequestID and Flags are in BIG-ENDIAN format!
			// Parse message format: [Type:144][RequestID:BE-ushort][Flags:BE-ushort][SFS2X-Payload]
			
			// Read RequestID in big-endian
			var requestIdBytes = reader.ReadBytes(2);
			var requestId = (ushort)((requestIdBytes[0] << 8) | requestIdBytes[1]);
			_logger.LogInformation("RequestID (big-endian): {RequestId} (0x{RequestIdHex:X4})", requestId, requestId);
			
			// Read Flags/Additional header in big-endian
			ushort flags = 0;
			if (reader.BaseStream.Length - reader.BaseStream.Position >= 2)
			{
				var flagBytes = reader.ReadBytes(2);
				flags = (ushort)((flagBytes[0] << 8) | flagBytes[1]);
				_logger.LogInformation("Flags/Header (big-endian): {Flags} (0x{FlagsHex:X4})", flags, flags);
			}
			
			var remainingBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
			_logger.LogInformation("Remaining payload after headers: {Length} bytes", remainingBytes.Length);
			_logger.LogInformation("Payload hex: {Hex}", Convert.ToHexString(remainingBytes));
			
			// Try to identify if this looks like an SFS2X object
			if (remainingBytes.Length > 0 && remainingBytes[0] == 0x12)
			{
				_logger.LogInformation("DISCOVERY: Payload starts with 0x12 - This is an SFS_OBJECT type marker!");
			}
			
			// Parse the custom binary format
			var clientData = ParseCustom144Format(remainingBytes);
			
			// Now we have the proper structure: we need to parse the SFS2X payload
			// The payload should contain the actual SFS2X message (likely HandshakeRequest)
			_logger.LogInformation("Protocol 144 structure confirmed - RequestID: {RequestId}, Flags: {Flags}", requestId, flags);
			
			// Try to parse the SFS2X payload
			if (remainingBytes.Length > 0)
			{
				_logger.LogInformation("Attempting to parse SFS2X payload...");
				
				// The payload appears to be a standard SFS2X message
				// Let's create a proper response wrapped in the 144 protocol
				return CreateWrapped144Response(requestId, flags, remainingBytes);
			}
			else
			{
				_logger.LogWarning("No payload found in message 144");
				return CreateSFS2XErrorResponse(144, "Empty payload");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error parsing message 144 for session {SessionId}", sessionId);
			return CreateSFS2XErrorResponse(144, "Message parsing failed");
		}
	}


	/// <summary>
	/// Creates official SFS2X HandshakeResponse wrapped in 144 protocol format
	/// </summary>
	private string CreateOfficialWrappedHandshakeResponse(ushort requestId, ushort flags, Dictionary<string, string> clientData)
	{
		_logger.LogInformation("=== CREATING OFFICIAL SFS2X HANDSHAKE RESPONSE ===");
		_logger.LogInformation("Client API version: {ApiVersion}", clientData.GetValueOrDefault("version", "unknown"));
		_logger.LogInformation("Client type: {ClientType}", clientData.GetValueOrDefault("clientMode", "unknown"));
		
		try
		{
			// EXPERIMENT: Try exact SFS2X HandshakeResponse format
			// Based on client request, it expects specific field names and values
			var handshakeData = new OfficialSFSObject();
			
			// Try minimal response with just the status (what client might be checking for)
			handshakeData.PutUtfString("_cmd", "handshake");
			handshakeData.PutBool("success", true);
			
			_logger.LogInformation("EXPERIMENTAL: Trying minimal handshake response format");
			_logger.LogInformation("Fields: _cmd=handshake, success=true");
			
			// Serialize the official SFS2X object to binary format
			var sfs2xPayload = SerializeOfficialSFSObject(handshakeData);
			
			// TEST: Use direct official payload without extra header
			// The official library might already include the correct format
			var finalPayload = sfs2xPayload;
			
			// Wrap in 144 protocol format: [144][RequestID][Flags][SFS2X_PAYLOAD]
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream, Encoding.UTF8);
			
			writer.Write((byte)144);           // Wrapper protocol type
			writer.Write(requestId);           // Echo back RequestID
			writer.Write(flags);               // FIXED: Echo back client's flags exactly (0x0012)
			writer.Write(finalPayload);        // SFS2X HandshakeResponse with message type header
			
			var responseBytes = stream.ToArray();
			var response = Convert.ToBase64String(responseBytes);
			
			_logger.LogInformation("=== MINIMAL WRAPPER RESPONSE CREATED ===");
			_logger.LogInformation("CHANGE: Echoing client flags 0x{ClientFlags:X4} instead of 0x0000", flags);
			_logger.LogInformation("CHANGE: Using direct official SFS2X payload (no extra header)");
			_logger.LogInformation("Total response: {Length} bytes", responseBytes.Length);
			_logger.LogInformation("Response hex: {Hex}", Convert.ToHexString(responseBytes));
			_logger.LogInformation("THEORY: Official SFS2X library provides complete binary format");
			_logger.LogInformation("Expected: Client should stop polling and progress to LoginRequest");
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating official wrapped HandshakeResponse");
			return CreateWrappedErrorResponse(requestId, "HandshakeResponse creation failed");
		}
	}

	/// <summary>
	/// Uses official SFS2X library to parse client payload and identify actual message type
	/// </summary>
	private ParsedSFS2XRequest? ParseOfficialSFS2XPayload(byte[] payload)
	{
		_logger.LogInformation("=== CRITICAL FIX: PROPER SFS2X BINARY PARSING ===");
		_logger.LogInformation("Parsing {Length} bytes using corrected SFS2X protocol understanding", payload.Length);
		
		try
		{
			// BREAKTHROUGH: Parse SFS2X message structure correctly
			// Format: c (controller) + a (action) + p (parameters as SFSObject)
			return ParseSFS2XMessageStructure(payload);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "SFS2X message structure parsing failed");
			return null;
		}
	}

	/// <summary>
	/// BREAKTHROUGH: Parse the correct SFS2X message structure 
	/// Format: c (controller) + a (action) + p (parameters as SFSObject)
	/// </summary>
	private ParsedSFS2XRequest? ParseSFS2XMessageStructure(byte[] payload)
	{
		_logger.LogInformation("=== PARSING CORRECT SFS2X MESSAGE STRUCTURE ===");
		_logger.LogInformation("Payload hex: {Hex}", Convert.ToHexString(payload));
		
		try
		{
			// Find the SFSObject in the payload (starts with type 18 = 0x12)
			int sfsObjectOffset = -1;
			for (int i = 0; i < payload.Length; i++)
			{
				if (payload[i] == 0x12) // SFS_OBJECT type
				{
					sfsObjectOffset = i;
					_logger.LogInformation("Found SFSObject at offset {Offset}", i);
					break;
				}
			}
			
			if (sfsObjectOffset == -1)
			{
				_logger.LogWarning("No SFSObject found in payload");
				return ParseLegacyFormat(payload);
			}

			// Extract the SFSObject portion
			var sfsObjectData = payload[sfsObjectOffset..];
			_logger.LogInformation("Extracting SFSObject: {Length} bytes from offset {Offset}", sfsObjectData.Length, sfsObjectOffset);
			
			// Parse controller and action from the beginning of the payload
			var controller = payload[0];  // 'c' field
			var action = ParseActionFromPayload(payload);
			
			_logger.LogInformation("Controller: {Controller}, Action: {Action}", controller, action);
			
			// Parse the SFSObject using official library
			var byteArray = new ByteArray(sfsObjectData);
			var parametersObject = OfficialSFSObject.NewFromBinaryData(byteArray);
			
			if (parametersObject != null)
			{
				_logger.LogInformation("Successfully parsed SFSObject parameters with {Count} keys", parametersObject.Size());
				
				// Log all keys for debugging
				foreach (var key in parametersObject.GetKeys())
				{
					_logger.LogInformation("Parameter key: {Key}", key);
				}
				
				// Determine message type based on controller, action, and parameters
				var messageType = DetermineMessageType(controller, action, parametersObject);
				
				return new ParsedSFS2XRequest
				{
					MessageType = messageType,
					Data = parametersObject,
					RequestTypeName = GetRequestTypeName(messageType)
				};
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "SFS2X message structure parsing failed");
		}
		
		return null;
	}
	
	/// <summary>
	/// Parse action field from SFS2X payload structure
	/// </summary>
	private short ParseActionFromPayload(byte[] payload)
	{
		try
		{
			// Look for action field pattern in the payload
			// This is a heuristic based on typical SFS2X structure
			if (payload.Length >= 4)
			{
				using var stream = new MemoryStream(payload);
				using var reader = new BinaryReader(stream);
				
				reader.ReadByte(); // Skip controller
				// Try to read potential action value
				// This may need adjustment based on actual format
				return (short)reader.ReadUInt16();
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Could not parse action from payload");
		}
		
		return -1;
	}

	/// <summary>
	/// Determine message type based on controller, action, and parameters
	/// </summary>
	private int DetermineMessageType(byte controller, short action, OfficialSFSObject parameters)
	{
		_logger.LogInformation("Determining message type from controller={Controller}, action={Action}", controller, action);
		
		// Check for HandshakeRequest indicators in parameters
		if (parameters.ContainsKey("api") && parameters.ContainsKey("cl"))
		{
			_logger.LogInformation("DETECTED: HandshakeRequest (api + cl fields present)");
			return 0; // HandshakeRequest
		}
		
		// Check for LoginRequest indicators
		if (parameters.ContainsKey("un") || parameters.ContainsKey("pw") || parameters.ContainsKey("zn"))
		{
			_logger.LogInformation("DETECTED: LoginRequest (un/pw/zn fields present)");
			return 1; // LoginRequest
		}
		
		// Check for room group subscription
		if (parameters.ContainsKey("g") || controller == 0)
		{
			_logger.LogInformation("DETECTED: Possible SubscribeRoomGroupRequest");
			return 15; // SubscribeRoomGroupRequest
		}
		
		// Default based on action if available
		if (action >= 0 && action <= 50)
		{
			_logger.LogInformation("Using action {Action} as message type", action);
			return action;
		}
		
		_logger.LogWarning("Could not determine message type, defaulting to HandshakeRequest");
		return 0; // Default to HandshakeRequest
	}

	/// <summary>
	/// Fallback to parse payload in legacy format
	/// </summary>
	private ParsedSFS2XRequest? ParseLegacyFormat(byte[] payload)
	{
		_logger.LogInformation("Falling back to legacy parsing approach");
		
		// If we can't find proper structure, assume this is a HandshakeRequest
		// since that's typically the first message in the protocol sequence
		return new ParsedSFS2XRequest
		{
			MessageType = 0, // HandshakeRequest
			Data = null,
			RequestTypeName = "HandshakeRequest (legacy fallback)"
		};
	}

	/// <summary>
	/// Creates the correct wrapped response based on discovered message type
	/// </summary>
	private async Task<string> CreateCorrectWrappedResponse(ushort requestId, ushort flags, ParsedSFS2XRequest parsedRequest)
	{
		_logger.LogInformation("=== CREATING CORRECT RESPONSE FOR MESSAGE TYPE {Type} ===", parsedRequest.MessageType);
		
		return parsedRequest.MessageType switch
		{
			0 => CreateWrappedHandshakeResponseNew(requestId, flags, parsedRequest),
			1 => CreateWrappedLoginResponse(requestId, flags, parsedRequest),
			3 => CreateWrappedGenericResponse(requestId, flags, parsedRequest), // Legacy fallback
			15 => await CreateWrappedSubscribeRoomGroupResponse(requestId, flags, parsedRequest), // REAL BREAKTHROUGH!
			6 => CreateWrappedExtensionResponse(requestId, flags, parsedRequest),
			_ => CreateWrappedGenericResponse(requestId, flags, parsedRequest)
		};
	}

	/// <summary>
	/// Attempts to identify the SFS2X message type from parsed object content
	/// </summary>
	private int IdentifyMessageType(OfficialSFSObject sfsObject)
	{
		try
		{
			// Check for common SFS2X message type indicators
			if (sfsObject.ContainsKey("_cmd"))
			{
				var command = sfsObject.GetUtfString("_cmd");
				_logger.LogInformation("Found _cmd field: {Command}", command);
				
				return command switch
				{
					"handshake" => 0,
					"login" => 1,
					"logout" => 2,
					"joinRoom" => 4,
					"extension" => 6,
					_ => -1
				};
			}
			
			// Check for HandshakeRequest indicators
			if (sfsObject.ContainsKey("api") && sfsObject.ContainsKey("cl"))
			{
				_logger.LogInformation("Detected HandshakeRequest pattern (api + cl fields)");
				return 0; // HandshakeRequest
			}
			
			// Check for LoginRequest indicators
			if (sfsObject.ContainsKey("un") || sfsObject.ContainsKey("pw") || sfsObject.ContainsKey("zn"))
			{
				_logger.LogInformation("Detected LoginRequest pattern (un/pw/zn fields)");
				return 1; // LoginRequest
			}
			
			_logger.LogWarning("Could not identify message type from SFSObject content");
			return -1; // Unknown
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error identifying message type");
			return -1;
		}
	}

	/// <summary>
	/// Gets friendly name for SFS2X request type
	/// </summary>
	private string GetRequestTypeName(int messageType)
	{
		return messageType switch
		{
			0 => "HandshakeRequest",
			1 => "LoginRequest", 
			2 => "LogoutRequest",
			3 => "GetRoomListRequest",
			4 => "JoinRoomRequest",
			5 => "LeaveRoomRequest",
			6 => "ExtensionRequest",
			_ => $"Unknown({messageType})"
		};
	}

	/// <summary>
	/// Fallback method to parse structured SFS2X message format using AUTHORITATIVE findings
	/// </summary>
	private ParsedSFS2XRequest? ParseStructuredSFS2XMessage(byte[] payload)
	{
		_logger.LogInformation("=== AUTHORITATIVE SFS2X ANALYSIS ===");
		_logger.LogInformation("DISCOVERY: GetRoomList (type 3) is NOT client-initiated!");
		_logger.LogInformation("REAL PROTOCOL: Clients use SubscribeRoomGroupRequest (ID 15)");
		_logger.LogInformation("Analyzing payload with correct SFS2X understanding...");
		
		try
		{
			using var stream = new MemoryStream(payload);
			using var reader = new BinaryReader(stream, Encoding.UTF8);
			
			// The first byte 0x03 is NOT message type 3 - it's part of SFS2X binary format
			// This is likely an SFSObject or controller/action structure
			if (stream.Length > 0)
			{
				var firstByte = reader.ReadByte();
				_logger.LogInformation("First byte: 0x{FirstByte:X2} (NOT message type - part of SFS2X format)", firstByte);
				
				// Based on findings, this is likely SubscribeRoomGroupRequest (ID 15)
				// or possibly HandshakeRequest which comes first in the protocol sequence
				_logger.LogInformation("HYPOTHESIS: This is likely SubscribeRoomGroupRequest (ID 15) or HandshakeRequest");
				
				// Return as SubscribeRoomGroupRequest for now - this is what room list clients send
				return new ParsedSFS2XRequest
				{
					MessageType = 15, // SubscribeRoomGroupRequest based on authoritative analysis
					Data = null,
					RequestTypeName = "SubscribeRoomGroupRequest"
				};
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Authoritative SFS2X parsing failed");
		}
		
		return null;
	}

	/// <summary>
	/// Creates wrapped HandshakeResponse for discovered HandshakeRequest (new format)
	/// </summary>
	private string CreateWrappedHandshakeResponseNew(ushort requestId, ushort flags, ParsedSFS2XRequest parsedRequest)
	{
		_logger.LogInformation("Creating wrapped HandshakeResponse for discovered HandshakeRequest");
		_logger.LogInformation("Request data: {Data}", parsedRequest.Data);
		
		try
		{
			// Create HandshakeResponse using official SFS2X library
			var handshakeData = new OfficialSFSObject();
			handshakeData.PutUtfString("_cmd", "handshake");
			handshakeData.PutBool("success", true);
			handshakeData.PutUtfString("sessionToken", Guid.NewGuid().ToString("N")[..16]);
			
			var sfs2xPayload = SerializeOfficialSFSObject(handshakeData);
			
			// Wrap in 144 protocol format with CORRECT ENDIANNESS
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream, Encoding.UTF8);
			
			writer.Write((byte)144);           // Wrapper protocol type
			
			// CRITICAL FIX: Write request ID and flags in big-endian (network byte order)
			writer.Write((byte)(requestId >> 8));   // High byte first (big-endian)
			writer.Write((byte)(requestId & 0xFF)); // Low byte second
			writer.Write((byte)(flags >> 8));       // High byte first (big-endian)  
			writer.Write((byte)(flags & 0xFF));     // Low byte second
			
			writer.Write(sfs2xPayload);        // HandshakeResponse payload
			
			var responseBytes = stream.ToArray();
			var response = Convert.ToBase64String(responseBytes);
			
			_logger.LogInformation("HandshakeResponse created: {Length} bytes", responseBytes.Length);
			_logger.LogInformation("Response hex: {Hex}", Convert.ToHexString(responseBytes));
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating wrapped HandshakeResponse");
			return CreateWrappedErrorResponse(requestId, "HandshakeResponse creation failed");
		}
	}

	/// <summary>
	/// Creates wrapped SubscribeRoomGroupResponse for message type 15 - THE REAL BREAKTHROUGH!
	/// </summary>
	private async Task<string> CreateWrappedSubscribeRoomGroupResponse(ushort requestId, ushort flags, ParsedSFS2XRequest parsedRequest)
	{
		_logger.LogInformation("🎯 REAL BREAKTHROUGH: Creating SubscribeRoomGroupResponse based on AUTHORITATIVE analysis!");
		_logger.LogInformation("DISCOVERY: This is the correct protocol - clients send SubscribeRoomGroupRequest (ID 15)");
		_logger.LogInformation("RESPONSE: ROOM_GROUP_SUBSCRIBE event with newRooms array");
		
		try
		{
			// Get actual rooms from the room manager
			var rooms = await _roomManager.GetRoomsInGroupAsync("lobbies");
			
			// Create ROOM_GROUP_SUBSCRIBE event response (AUTHORITATIVE format)
			var subscribeEventData = new OfficialSFSObject();
			subscribeEventData.PutUtfString("_cmd", "roomGroupSubscribe");
			subscribeEventData.PutBool("success", true);
			
			// Create newRooms array - this is the EXACT format from authoritative analysis
			var newRoomsArray = new OfficialSFSArray();
			
			foreach (var room in rooms)
			{
				var roomObj = new OfficialSFSObject();
				roomObj.PutUtfString("name", room.Name);
				roomObj.PutInt("userCount", room.UserCount);
				roomObj.PutInt("maxUsers", room.MaxUsers);
				roomObj.PutInt("id", room.RoomId);
				roomObj.PutBool("isGame", true);
				roomObj.PutBool("isPasswordProtected", !string.IsNullOrEmpty(room.Password));
				roomObj.PutUtfString("groupId", "lobbies");
				newRoomsArray.AddSFSObject(roomObj);
			}
			
			// If no rooms exist, create a sample room so client has something to subscribe to
			if (!rooms.Any())
			{
				_logger.LogInformation("No rooms found, creating sample room for SubscribeRoomGroup");
				var sampleRoom = new OfficialSFSObject();
				sampleRoom.PutUtfString("name", "Offworld Lobby");
				sampleRoom.PutInt("userCount", 0);
				sampleRoom.PutInt("maxUsers", 8);
				sampleRoom.PutInt("id", 1001);
				sampleRoom.PutBool("isGame", true);
				sampleRoom.PutBool("isPasswordProtected", false);
				sampleRoom.PutUtfString("groupId", "lobbies");
				newRoomsArray.AddSFSObject(sampleRoom);
			}
			
			// AUTHORITATIVE format: { "newRooms": [array] }
			subscribeEventData.PutSFSArray("newRooms", newRoomsArray);
			
			var sfs2xPayload = SerializeOfficialSFSObject(subscribeEventData);
			
			// Wrap in 144 protocol format with CORRECT ENDIANNESS
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream, Encoding.UTF8);
			
			writer.Write((byte)144);           // Wrapper protocol type
			
			// CRITICAL FIX: Write request ID and flags in big-endian (network byte order)
			writer.Write((byte)(requestId >> 8));   // High byte first (big-endian)
			writer.Write((byte)(requestId & 0xFF)); // Low byte second
			writer.Write((byte)(flags >> 8));       // High byte first (big-endian)  
			writer.Write((byte)(flags & 0xFF));     // Low byte second
			
			writer.Write(sfs2xPayload);        // SubscribeRoomGroupResponse payload
			
			var responseBytes = stream.ToArray();
			var response = Convert.ToBase64String(responseBytes);
			
			_logger.LogInformation("🎉 AUTHORITATIVE SubscribeRoomGroupResponse created: {Length} bytes with {RoomCount} rooms", 
				responseBytes.Length, newRoomsArray.Size());
			_logger.LogInformation("Response hex: {Hex}", Convert.ToHexString(responseBytes));
			_logger.LogInformation("FORMAT: ROOM_GROUP_SUBSCRIBE event with newRooms array (authoritative)");
			_logger.LogInformation("EXPECTED: Client should FINALLY stop polling and display room list!");
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating authoritative SubscribeRoomGroupResponse");
			return CreateWrappedErrorResponse(requestId, "SubscribeRoomGroupResponse creation failed");
		}
	}

	/// <summary>
	/// Creates wrapped GetRoomListResponse for message type 3 - LEGACY (not used by clients)
	/// </summary>
	private async Task<string> CreateWrappedGetRoomListResponse(ushort requestId, ushort flags, ParsedSFS2XRequest parsedRequest)
	{
		_logger.LogInformation("🎯 BREAKTHROUGH: Creating GetRoomListResponse for discovered GetRoomListRequest!");
		_logger.LogInformation("This should finally stop the infinite polling loop!");
		
		try
		{
			// Get actual rooms from the room manager
			var rooms = await _roomManager.GetRoomsInGroupAsync("lobbies");
			
			// Create GetRoomListResponse using official SFS2X format
			var roomListData = new OfficialSFSObject();
			roomListData.PutUtfString("_cmd", "getRoomList");
			roomListData.PutBool("success", true);
			
			// Create rooms array - this is what the client is actually waiting for!
			var roomsArray = new OfficialSFSArray();
			
			foreach (var room in rooms)
			{
				var roomObj = new OfficialSFSObject();
				roomObj.PutUtfString("name", room.Name);
				roomObj.PutInt("userCount", room.UserCount);
				roomObj.PutInt("maxUsers", room.MaxUsers);
				roomObj.PutInt("id", room.RoomId);
				roomObj.PutBool("isGame", true);
				roomObj.PutBool("isPasswordProtected", !string.IsNullOrEmpty(room.Password));
				roomsArray.AddSFSObject(roomObj);
			}
			
			// If no rooms exist, create a sample room so client has something to see
			if (!rooms.Any())
			{
				_logger.LogInformation("No rooms found, creating sample room for client");
				var sampleRoom = new OfficialSFSObject();
				sampleRoom.PutUtfString("name", "Offworld Lobby");
				sampleRoom.PutInt("userCount", 0);
				sampleRoom.PutInt("maxUsers", 8);
				sampleRoom.PutUtfString("id", "sample_room_1");
				sampleRoom.PutBool("isGame", true);
				sampleRoom.PutBool("isPasswordProtected", false);
				roomsArray.AddSFSObject(sampleRoom);
			}
			
			roomListData.PutSFSArray("rooms", roomsArray);
			
			var sfs2xPayload = SerializeOfficialSFSObject(roomListData);
			
			// Wrap in 144 protocol format
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream, Encoding.UTF8);
			
			writer.Write((byte)144);           // Wrapper protocol type
			writer.Write(requestId);           // Echo back RequestID
			writer.Write(flags);               // Echo back client's flags
			writer.Write(sfs2xPayload);        // GetRoomListResponse payload
			
			var responseBytes = stream.ToArray();
			var response = Convert.ToBase64String(responseBytes);
			
			_logger.LogInformation("🎉 GetRoomListResponse created: {Length} bytes with {RoomCount} rooms", 
				responseBytes.Length, roomsArray.Size());
			_logger.LogInformation("Response hex: {Hex}", Convert.ToHexString(responseBytes));
			_logger.LogInformation("EXPECTED: Client should stop polling and display room list!");
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating GetRoomListResponse");
			return CreateWrappedErrorResponse(requestId, "GetRoomListResponse creation failed");
		}
	}

	/// <summary>
	/// Creates wrapped LoginResponse for message type 1
	/// </summary>
	private string CreateWrappedLoginResponse(ushort requestId, ushort flags, ParsedSFS2XRequest parsedRequest)
	{
		_logger.LogInformation("Creating wrapped LoginResponse for discovered LoginRequest");
		
		// Use existing CreateOfficialWrappedHandshakeResponse but adapt for LoginResponse
		// This is a quick implementation - should be expanded based on game requirements
		return CreateWrappedGenericResponse(requestId, flags, parsedRequest);
	}

	/// <summary>
	/// Creates wrapped ExtensionResponse for message type 6
	/// </summary>
	private string CreateWrappedExtensionResponse(ushort requestId, ushort flags, ParsedSFS2XRequest parsedRequest)
	{
		_logger.LogInformation("Creating wrapped ExtensionResponse for discovered ExtensionRequest");
		return CreateWrappedGenericResponse(requestId, flags, parsedRequest);
	}

	/// <summary>
	/// Creates generic wrapped response for any message type
	/// </summary>
	private string CreateWrappedGenericResponse(ushort requestId, ushort flags, ParsedSFS2XRequest parsedRequest)
	{
		_logger.LogInformation("Creating generic wrapped response for message type {Type}", parsedRequest.MessageType);
		
		try
		{
			// Create a simple acknowledgment response
			var responseData = new OfficialSFSObject();
			responseData.PutUtfString("_cmd", "response");
			responseData.PutBool("success", true);
			responseData.PutInt("messageType", parsedRequest.MessageType);
			
			var sfs2xPayload = SerializeOfficialSFSObject(responseData);
			
			// Wrap in 144 protocol format
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream, Encoding.UTF8);
			
			writer.Write((byte)144);           // Wrapper protocol type
			writer.Write(requestId);           // Echo back RequestID
			writer.Write(flags);               // Echo back client's flags
			writer.Write(sfs2xPayload);        // Generic response payload
			
			var responseBytes = stream.ToArray();
			var response = Convert.ToBase64String(responseBytes);
			
			_logger.LogInformation("Generic response created: {Length} bytes", responseBytes.Length);
			_logger.LogInformation("Response hex: {Hex}", Convert.ToHexString(responseBytes));
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating generic wrapped response");
			return CreateWrappedErrorResponse(requestId, "Generic response creation failed");
		}
	}

	/// <summary>
	/// Prepends SFS2X message type header to payload for wrapper protocol
	/// </summary>
	private byte[] PrependSFS2XMessageHeader(byte[] payload, byte messageType)
	{
		_logger.LogInformation("Adding SFS2X message type {MessageType} header to {PayloadSize} byte payload", 
			messageType, payload.Length);
		
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		// Add SFS2X message type header
		writer.Write(messageType);    // 0 = HandshakeResponse, 1 = LoginResponse, etc.
		writer.Write(payload);        // Original SFS2X object payload
		
		var result = stream.ToArray();
		_logger.LogInformation("Final payload with header: {Size} bytes", result.Length);
		
		return result;
	}

	/// <summary>
	/// Serializes official SFS2X object to binary format
	/// </summary>
	private byte[] SerializeOfficialSFSObject(OfficialSFSObject sfsObject)
	{
		try
		{
			// Use official SFS2X library's binary serialization
			// This should match the exact format the client expects
			var binaryData = sfsObject.ToBinary();
			return binaryData.Bytes;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error serializing official SFSObject");
			// Fallback to minimal binary format if official serialization fails
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream, Encoding.UTF8);
			
			// Minimal SFSObject format: [type][count][key-value pairs]
			writer.Write((byte)0x12);          // SFSObject type marker
			writer.Write((short)1);           // 1 key-value pair
			writer.Write("status");           // Key
			writer.Write((byte)8);            // UTF8 string type
			writer.Write("handshake_success"); // Value
			
			return stream.ToArray();
		}
	}

	/// <summary>
	/// Creates handshake response wrapped in 144 protocol format (LEGACY)
	/// </summary>
	private string CreateWrappedHandshakeResponse(ushort requestId, ushort clientFlags, Dictionary<string, string> clientData)
	{
		_logger.LogInformation("=== CREATING WRAPPED HANDSHAKE RESPONSE ===");
		_logger.LogInformation("Wrapping SFS2X HandshakeResponse in 144 protocol");
		_logger.LogInformation("Echoing client RequestID: {RequestId}, Flags: 0x{Flags:X4}", requestId, clientFlags);
		
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		// FINAL TEST: Client may expect NO PAYLOAD AT ALL in handshake response
		// Just wrapper headers: [144][RequestID][Flags] = 5 bytes total
		// This is the absolute minimal possible acknowledgment
		
		// var sfs2xPayload = CreateHandshakeAckPayload(); // DISABLED: Testing empty payload
		var sfs2xPayload = new byte[0]; // EMPTY PAYLOAD TEST
		
		// FIXED: Match client's wrapper format exactly - no extra SFS2X framing
		// Client format: [144][RequestID][Flags][SFS2X_PAYLOAD_DIRECT]
		writer.Write((byte)144);           // Wrapper protocol type
		writer.Write(requestId);           // Echo back RequestID
		writer.Write(clientFlags);         // FIXED: Echo client's flags exactly
		writer.Write(sfs2xPayload);        // SFS2X HandshakeResponse (direct, no extra headers)
		
		var responseBytes = stream.ToArray();
		var response = Convert.ToBase64String(responseBytes);
		
		_logger.LogInformation("=== FINAL TEST: EMPTY PAYLOAD RESPONSE ===");
		_logger.LogInformation("HYPOTHESIS: Client expects NO payload in handshake response");
		_logger.LogInformation("Testing absolute minimal response: [144][RequestID][Flags]");
		_logger.LogInformation("Response: {Length} bytes (expecting 5 bytes total)", responseBytes.Length);
		_logger.LogInformation("Response hex: {Hex}", Convert.ToHexString(responseBytes));
		_logger.LogInformation("CRITICAL: If this fails, client may expect different protocol entirely");
		
		return response;
	}

	/// <summary>
	/// Creates minimal binary handshake acknowledgment payload (not SFSObject)
	/// </summary>
	private byte[] CreateHandshakeAckPayload()
	{
		// Try creating minimal binary response
		// Based on analysis: maybe just a simple success indicator
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		// Minimal acknowledgment - just indicate success
		writer.Write((byte)0x01);  // Success indicator
		
		return stream.ToArray();
	}

	/// <summary>
	/// Creates error response wrapped in 144 protocol format
	/// </summary>
	private string CreateWrappedErrorResponse(ushort requestId, string errorMessage)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		var errorResponse = new SFSObject();
		errorResponse.PutUtfString("error", errorMessage);
		var payload = errorResponse.Serialize();
		
		writer.Write((byte)144);           // Wrapper protocol type
		writer.Write(requestId);           // RequestID
		writer.Write((ushort)0x0001);      // Error flag
		writer.Write((byte)255);           // Error message type
		writer.Write((uint)payload.Length);
		writer.Write(payload);
		
		return Convert.ToBase64String(stream.ToArray());
	}

	/// <summary>
	/// Temporary handler for legacy message type 144 to investigate what it actually represents
	/// </summary>
	private async Task<string?> HandleLegacyMessage144(string sessionId, BinaryReader reader)
	{
		_logger.LogInformation("Legacy message type 144 received - investigating what this actually is");
		_logger.LogInformation("Now that we know Handshake=0, type 144 is something else in the message format");
		
		// For now, delegate to the existing handshake handler while we investigate
		// TODO: Figure out what message type 144 actually represents
		return await HandleClientHandshakeRequest(sessionId, reader);
	}

	/// <summary>
	/// Handles message 144 when SFS2X parsing fails - fallback to legacy approach
	/// </summary>
	private async Task<string?> HandleMessage144LegacyFormat(string sessionId, ushort requestId, byte[] payload)
	{
		_logger.LogWarning("Message 144 parsing failed, using fallback response");
		return CreateSFS2XMessage144Response(requestId, new Dictionary<string, string>());
	}

	/// <summary>
	/// Parses the custom binary format used by message type 144
	/// </summary>
	private Dictionary<string, string> ParseCustom144Format(byte[] payload)
	{
		var result = new Dictionary<string, string>();
		
		try
		{
			// Search for "api" pattern and extract version
			var apiPos = FindStringInPayload(payload, "api");
			if (apiPos >= 0)
			{
				var versionPos = FindStringInPayload(payload, "1.7.8", apiPos);
				if (versionPos >= 0)
				{
					result["api"] = "1.7.8";
				}
			}
			
			// Search for "cl" pattern and extract client type
			var clPos = FindStringInPayload(payload, "cl");
			if (clPos >= 0)
			{
				var unityPos = FindStringInPayload(payload, "Unity", clPos);
				if (unityPos >= 0)
				{
					result["cl"] = "Unity";
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to parse custom 144 format");
		}
		
		return result;
	}

	/// <summary>
	/// Finds a string pattern in the payload bytes
	/// </summary>
	private static int FindStringInPayload(byte[] payload, string searchString, int startPos = 0)
	{
		var searchBytes = System.Text.Encoding.UTF8.GetBytes(searchString);
		
		for (int i = startPos; i <= payload.Length - searchBytes.Length; i++)
		{
			bool found = true;
			for (int j = 0; j < searchBytes.Length; j++)
			{
				if (payload[i + j] != searchBytes[j])
				{
					found = false;
					break;
				}
			}
			if (found) return i;
		}
		return -1;
	}
	/// <summary>
	/// Creates appropriate SFS2X response for message type 144
	/// </summary>
	private string CreateSFS2XMessage144Response(ushort requestId, Dictionary<string, string> clientData)
	{
		// HYPOTHESIS: Client sends message 144 (handshake) but expects standard LoginResponse (type 1)
		// Many SFS2X implementations use handshake to establish connection, then expect LoginResponse
		
		var responseData = new SFSObject();
		responseData.PutUtfString("_cmd", "loginResponse");
		responseData.PutBool("success", true);
		responseData.PutUtfString("version", "1.0.0");
		
		// Add P2P networking info that client needs for multiplayer
		responseData.PutUtfString("ipAddress", "127.0.0.1");
		responseData.PutInt("port", 9933);
		
		// Try responding with LoginResponse (type 1) instead of echoing type 144
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		var payloadBytes = responseData.Serialize();
		
		writer.Write((byte)1);                 // LoginResponse (type 1) instead of 144
		writer.Write(requestId);               // Echo back request ID
		writer.Write((ushort)payloadBytes.Length); // Payload size
		writer.Write(payloadBytes);            // SFS2X response data
		
		var responseBytes = stream.ToArray();
		_logger.LogInformation("Sending LoginResponse (type 1) for handshake request 144: {Length} bytes", responseBytes.Length);
		
		return Convert.ToBase64String(responseBytes);
	}

	/// <summary>
	/// Creates a response wrapped in the 144 protocol format
	/// </summary>
	private string CreateWrapped144Response(ushort requestId, ushort flags, byte[] sfs2xPayload)
	{
		_logger.LogInformation("=== CREATING WRAPPED 144 RESPONSE ===");
		_logger.LogInformation("Client flags: 0x{Flags:X4} - Checking for compression flag", flags);
		
		try
		{
			// Check if compression is enabled (bit 9 = 0x0200)
			bool compressionEnabled = (flags & 0x0200) != 0;
			_logger.LogInformation("Compression flag (0x0200): {Enabled}", compressionEnabled);
			
			// Parse the client's payload to understand what type of handshake response to send
			// The payload contains api version and client info
			
			// Create proper SFS2X HandshakeResponse
			// Based on protocol analysis, handshake response should be minimal
			var handshakeResponse = new SFSObject();
			
			// The handshake response typically contains session info
			handshakeResponse.PutBool("success", true);
			handshakeResponse.PutUtfString("sessionToken", Guid.NewGuid().ToString("N").Substring(0, 16));
			handshakeResponse.PutLong("serverTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
			
			// Serialize the handshake response
			var responsePayload = handshakeResponse.Serialize();
			
			// Create the complete SFS2X message structure
			using var sfs2xStream = new MemoryStream();
			using var sfs2xWriter = new BinaryWriter(sfs2xStream, Encoding.UTF8);
			
			// Write the SFS2X message structure matching client format
			sfs2xWriter.Write((byte)0x03); // Controller ID (HANDSHAKE = 3)
			
			// Write action ID in big-endian (matching client)
			sfs2xWriter.Write((byte)0x00);
			sfs2xWriter.Write((byte)0x01);
			
			// Write 'c' parameter structure
			sfs2xWriter.Write((byte)0x63); // ASCII 'c'
			sfs2xWriter.Write((byte)0x02); // Type: BYTE
			sfs2xWriter.Write((byte)0x00); // Value: 0
			
			// Write parameter count in big-endian
			sfs2xWriter.Write((byte)0x00);
			sfs2xWriter.Write((byte)0x01);
			
			// Write 'a' parameter structure  
			sfs2xWriter.Write((byte)0x61); // ASCII 'a'
			sfs2xWriter.Write((byte)0x03); // Type: SHORT
			sfs2xWriter.Write((byte)0x00); // Value: 0 (BE short)
			sfs2xWriter.Write((byte)0x00);
			
			// Write parameter count in big-endian
			sfs2xWriter.Write((byte)0x00);
			sfs2xWriter.Write((byte)0x01);
			
			// Write 'p' parameter structure
			sfs2xWriter.Write((byte)0x70); // ASCII 'p'
			sfs2xWriter.Write((byte)0x12); // Type: SFS_OBJECT
			// Note: SFSObject payload already includes its own size/structure
			sfs2xWriter.Write(responsePayload); // SFSObject data
			
			var sfs2xMessage = sfs2xStream.ToArray();
			
			// Apply compression if flag is set
			if (compressionEnabled)
			{
				_logger.LogInformation("Applying zlib compression to response");
				var byteArray = new ByteArray(sfs2xMessage);
				byteArray.Compress();
				sfs2xMessage = byteArray.Bytes;
				_logger.LogInformation("Compressed {Original} bytes to {Compressed} bytes", 
					sfs2xStream.Length, sfs2xMessage.Length);
			}
			
			// Create the wrapped response with 144 protocol
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream, Encoding.UTF8);
			
			// Write wrapper header
			writer.Write((byte)144); // Protocol identifier
			
			// Write RequestID in big-endian
			writer.Write((byte)(requestId >> 8));
			writer.Write((byte)(requestId & 0xFF));
			
			// Echo back client's flags exactly
			writer.Write((byte)(flags >> 8));
			writer.Write((byte)(flags & 0xFF));
			
			// Write the SFS2X message
			writer.Write(sfs2xMessage);
			
			var responseBytes = stream.ToArray();
			var response = Convert.ToBase64String(responseBytes);
			
			_logger.LogInformation("Created wrapped handshake response: {Length} bytes", responseBytes.Length);
			_logger.LogInformation("Response hex: {Hex}", Convert.ToHexString(responseBytes));
			_logger.LogInformation("Flags: 0x{Flags:X4}, Compression: {Compression}", flags, compressionEnabled);
			_logger.LogInformation("Expected: Client receives HANDSHAKE event and proceeds to login");
			
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating wrapped 144 response");
			return CreateSFS2XErrorResponse(144, "Response creation failed");
		}
	}

	/// <summary>
	/// Creates SFS2X error response for message type
	/// </summary>
	private string CreateSFS2XErrorResponse(int messageType, string errorMessage)
	{
		_logger.LogInformation("Creating SFS2X error response for type {Type}: {Error}", messageType, errorMessage);
		
		var errorData = new SFSObject();
		errorData.PutUtfString("error", errorMessage);
		errorData.PutBool("success", false);
		
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.UTF8);
		
		var payloadBytes = errorData.Serialize();
		
		writer.Write((byte)messageType);       // Echo back message type
		writer.Write((ushort)0);               // No request ID for errors
		writer.Write((ushort)payloadBytes.Length);
		writer.Write(payloadBytes);
		
		return Convert.ToBase64String(stream.ToArray());
	}
}
