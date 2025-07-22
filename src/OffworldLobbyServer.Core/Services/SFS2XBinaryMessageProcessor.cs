using System.Text;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Models;
using OffworldLobbyServer.Shared.Enums;
using OffworldLobbyServer.Shared.SFS2X;

namespace OffworldLobbyServer.Core.Services;

/// <summary>
/// Processes native SFS2X binary protocol messages from direct TCP connections.
/// This handles the actual binary format used by SmartFoxServer, not HTTP-wrapped messages.
/// </summary>
public class SFS2XBinaryMessageProcessor
{
	private readonly ISessionManager _sessionManager;
	private readonly IRoomManager _roomManager;
	private readonly ILogger<SFS2XBinaryMessageProcessor> _logger;

	public SFS2XBinaryMessageProcessor(
		ISessionManager sessionManager,
		IRoomManager roomManager,
		ILogger<SFS2XBinaryMessageProcessor> logger)
	{
		_sessionManager = sessionManager;
		_roomManager = roomManager;
		_logger = logger;
	}

	/// <summary>
	/// Processes a native SFS2X binary message.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="messageData">Raw binary message data.</param>
	/// <returns>Binary response data or null.</returns>
	public async Task<byte[]?> ProcessBinaryMessage(string sessionId, byte[] messageData)
	{
		try
		{
			_logger.LogDebug(
				"Processing SFS2X binary message for session {SessionId}: {Length} bytes",
				sessionId, messageData.Length);

			using var stream = new MemoryStream(messageData);
			using var reader = new BinaryReader(stream, Encoding.UTF8);

			// Read SFS2X message header
			var header = ReadSFS2XHeader(reader);
			
			_logger.LogDebug(
				"SFS2X message - Type: {MessageType}, RequestId: {RequestId}, Size: {Size}",
				header.MessageType, header.RequestId, header.MessageSize);

			return header.MessageType switch
			{
				SFS2XMessageType.LoginRequest => await HandleBinaryLoginRequest(sessionId, reader, header),
				SFS2XMessageType.CreateRoomRequest => await HandleBinaryCreateRoomRequest(sessionId, reader, header),
				SFS2XMessageType.JoinRoomRequest => await HandleBinaryJoinRoomRequest(sessionId, reader, header),
				SFS2XMessageType.LeaveRoomRequest => await HandleBinaryLeaveRoomRequest(sessionId, reader, header),
				SFS2XMessageType.SetRoomVariablesRequest => await HandleBinarySetRoomVariablesRequest(sessionId, reader, header),
				SFS2XMessageType.ExtensionRequest => await HandleBinaryExtensionRequest(sessionId, reader, header),
				_ => await HandleUnknownBinaryMessage(sessionId, header.MessageType, reader, header)
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing SFS2X binary message for session {SessionId}", sessionId);
			return CreateBinaryErrorResponse("Processing error");
		}
	}

	/// <summary>
	/// Reads the SFS2X message header from the binary stream.
	/// </summary>
	private SFS2XMessageHeader ReadSFS2XHeader(BinaryReader reader)
	{
		// SFS2X binary format (based on SmartFoxServer documentation):
		// [1 byte: Message Type] [2 bytes: Request ID] [4 bytes: Message Size] [payload...]
		var messageType = (SFS2XMessageType)reader.ReadByte();
		var requestId = reader.ReadUInt16();
		var messageSize = reader.ReadUInt32();

		return new SFS2XMessageHeader
		{
			MessageType = messageType,
			RequestId = requestId,
			MessageSize = messageSize
		};
	}

	/// <summary>
	/// Handles binary SFS2X LoginRequest.
	/// </summary>
	private async Task<byte[]> HandleBinaryLoginRequest(string sessionId, BinaryReader reader, SFS2XMessageHeader header)
	{
		try
		{
			_logger.LogInformation("Processing binary LoginRequest for session {SessionId}", sessionId);

			// Read SFS2X LoginRequest structure: userName, password, zone, parameters
			var userName = ReadSFS2XString(reader);
			var password = ReadSFS2XString(reader);
			var zone = ReadSFS2XString(reader);
			
			// Read optional parameters (SFSObject with player data)
			var hasParams = reader.ReadBoolean();
			SFSObject? parameters = null;
			if (hasParams)
			{
				parameters = SFSObject.Deserialize(reader);
			}

			_logger.LogDebug("Binary LoginRequest - User: {UserName}, Zone: {Zone}", userName, zone);

			// Validate zone matches expected "Offworld" zone from game
			if (zone != "Offworld")
			{
				_logger.LogWarning("Invalid zone '{Zone}' in LoginRequest, expected 'Offworld'", zone);
				return CreateBinaryErrorResponse("Invalid zone");
			}

			// Get session info for IP address
			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session == null)
			{
				_logger.LogWarning("Session not found for binary LoginRequest: {SessionId}", sessionId);
				return CreateBinaryErrorResponse("Session not found");
			}

			// Parse player ID and extract additional player data from parameters
			var playerId = ParsePlayerIDFromString(userName);
			string playerName = userName;
			byte gender = 0;
			string tachyonId = string.Empty;
			
			if (parameters != null)
			{
				// Extract player data from parameters (based on decompiled game analysis)
				if (parameters.ContainsKey("playerName"))
					playerName = parameters.GetUtfString("playerName");
				if (parameters.ContainsKey("gender"))
					gender = parameters.GetByte("gender");
				if (parameters.ContainsKey("tachyonID"))
					tachyonId = parameters.GetUtfString("tachyonID");
					
				_logger.LogDebug("Player data - Name: {PlayerName}, Gender: {Gender}, TachyonID: {TachyonId}",
					playerName, gender, tachyonId);
			}

			// Update session with player info
			await _sessionManager.SetSessionPlayerAsync(sessionId, playerId);

			// Create binary login response with proper SFS2X format
			var loginResponse = new SFS2XLoginResponse
			{
				Success = true,
				RequestId = header.RequestId,
				Data = new SFSObject()
			};

			// Add critical P2P networking info (required by game for lobby-to-P2P transition)
			loginResponse.Data.PutUtfString("accessKey", "fake_aws_access_key");
			loginResponse.Data.PutUtfString("secret", "fake_aws_secret_key");
			loginResponse.Data.PutInt("serverTime", (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
			loginResponse.Data.PutUtfString("ipAddress", session.ClientIP); // CRITICAL for P2P
			loginResponse.Data.PutInt("port", GenerateP2PPort(session.ClientIP)); // CRITICAL for P2P
			
			// Add session/user information
			loginResponse.Data.PutUtfString("sessionId", sessionId);
			loginResponse.Data.PutUtfString("userName", userName);
			loginResponse.Data.PutUtfString("playerName", playerName);
			loginResponse.Data.PutBool("loginSuccess", true);

			_logger.LogInformation(
				"Binary LoginResponse sent for {PlayerName} (ID: {UserName}) - IP: {IP}, Port: {Port}",
				playerName, userName, session.ClientIP, loginResponse.Data.GetInt("port"));

			return SerializeBinaryResponse(loginResponse);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling binary LoginRequest for session {SessionId}", sessionId);
			return CreateBinaryErrorResponse("Login failed");
		}
	}

	/// <summary>
	/// Handles binary SFS2X CreateRoomRequest.
	/// </summary>
	private async Task<byte[]> HandleBinaryCreateRoomRequest(string sessionId, BinaryReader reader, SFS2XMessageHeader header)
	{
		try
		{
			_logger.LogInformation("Processing binary CreateRoomRequest for session {SessionId}", sessionId);

			// Read SFS2X RoomSettings structure
			var roomName = ReadSFS2XString(reader);
			var maxUsers = reader.ReadInt16();
			var groupId = ReadSFS2XString(reader);
			var password = ReadSFS2XString(reader);
			var autoJoin = reader.ReadBoolean();

			_logger.LogDebug(
				"Binary CreateRoomRequest - Name: {RoomName}, MaxUsers: {MaxUsers}, Group: {GroupId}, AutoJoin: {AutoJoin}",
				roomName, maxUsers, groupId, autoJoin);

			// Read room variables
			var variableCount = reader.ReadInt32();
			SFSArray? lobbySettingsArray = null;

			for (int i = 0; i < variableCount; i++)
			{
				var varName = ReadSFS2XString(reader);
				var varType = reader.ReadByte();

				if (varName == "lobbySettings" && varType == 6) // SFSArray type
				{
					lobbySettingsArray = SFSArray.Deserialize(reader);
					_logger.LogDebug("Found binary lobbySettings array with {Size} elements", lobbySettingsArray.Size);
				}
				else
				{
					// Skip other variables
					SkipSFS2XVariableData(reader, varType);
				}
			}

			if (lobbySettingsArray == null)
			{
				_logger.LogWarning("Binary CreateRoomRequest missing lobbySettings array");
				return CreateBinaryErrorResponse("Invalid lobby settings");
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
				_logger.LogWarning("Session has no associated player for binary CreateRoomRequest: {SessionId}", sessionId);
				return CreateBinaryErrorResponse("Player not logged in");
			}

			// Create the room
			var room = await _roomManager.CreateRoomAsync(lobbySettings, session.PlayerId, password);
			await _sessionManager.SetSessionRoomAsync(sessionId, room.RoomId);

			_logger.LogInformation(
				"Room created successfully via binary protocol: {RoomName} (ID: {RoomId}) by {Owner}",
				roomName, room.RoomId, session.PlayerId);

			// Create binary room creation response
			var response = new SFS2XCreateRoomResponse
			{
				Success = true,
				RequestId = header.RequestId,
				Room = CreateSFS2XRoom(room),
				User = CreateSFS2XUser(session.PlayerId.ToString(), room.RoomId)
			};

			return SerializeBinaryResponse(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling binary CreateRoomRequest for session {SessionId}", sessionId);
			return CreateBinaryErrorResponse("Room creation failed");
		}
	}

	/// <summary>
	/// Handles other binary message types (placeholder implementations).
	/// </summary>
	private async Task<byte[]> HandleBinaryJoinRoomRequest(string sessionId, BinaryReader reader, SFS2XMessageHeader header)
	{
		_logger.LogDebug("Binary JoinRoomRequest for session {SessionId} - not implemented", sessionId);
		return CreateBinarySuccessResponse(header.RequestId, "Join room successful");
	}

	private async Task<byte[]> HandleBinaryLeaveRoomRequest(string sessionId, BinaryReader reader, SFS2XMessageHeader header)
	{
		_logger.LogDebug("Binary LeaveRoomRequest for session {SessionId} - not implemented", sessionId);
		return CreateBinarySuccessResponse(header.RequestId, "Leave room successful");
	}

	private async Task<byte[]> HandleBinarySetRoomVariablesRequest(string sessionId, BinaryReader reader, SFS2XMessageHeader header)
	{
		_logger.LogDebug("Binary SetRoomVariablesRequest for session {SessionId} - not implemented", sessionId);
		return CreateBinarySuccessResponse(header.RequestId, "Variables updated");
	}

	private async Task<byte[]> HandleBinaryExtensionRequest(string sessionId, BinaryReader reader, SFS2XMessageHeader header)
	{
		_logger.LogDebug("Binary ExtensionRequest for session {SessionId} - not implemented", sessionId);
		return CreateBinarySuccessResponse(header.RequestId, "Extension handled");
	}

	private async Task<byte[]?> HandleUnknownBinaryMessage(string sessionId, SFS2XMessageType messageType, BinaryReader reader, SFS2XMessageHeader header)
	{
		_logger.LogWarning(
			"Unknown SFS2X binary message type {MessageType} for session {SessionId}",
			messageType, sessionId);
		return null; // No response for unknown messages
	}

	/// <summary>
	/// Reads an SFS2X string from the binary stream.
	/// </summary>
	private string ReadSFS2XString(BinaryReader reader)
	{
		var length = reader.ReadUInt16();
		if (length == 0)
			return string.Empty;

		var bytes = reader.ReadBytes(length);
		return Encoding.UTF8.GetString(bytes);
	}

	/// <summary>
	/// Skips SFS2X variable data based on type.
	/// </summary>
	private void SkipSFS2XVariableData(BinaryReader reader, byte varType)
	{
		switch (varType)
		{
			case 1: // String
				ReadSFS2XString(reader);
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
			case 6: // SFSArray
				SFSArray.Deserialize(reader);
				break;
			case 7: // SFSObject
				SFSObject.Deserialize(reader);
				break;
		}
	}

	/// <summary>
	/// Serializes a binary response to bytes using proper SFS2X protocol format.
	/// Format: [Message Type: 1 byte][Request ID: 2 bytes][Message Size: 4 bytes][Payload]
	/// </summary>
	private byte[] SerializeBinaryResponse(object response)
	{
		// First, build the payload
		byte[] payload;
		SFS2XMessageType messageType;
		ushort requestId;

		using (var payloadStream = new MemoryStream())
		using (var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8))
		{
			if (response is SFS2XLoginResponse loginResp)
			{
				messageType = SFS2XMessageType.LoginResponse;
				requestId = loginResp.RequestId;
				
				// Write LoginResponse payload: success flag + data object
				payloadWriter.Write(loginResp.Success);
				loginResp.Data.Serialize(payloadWriter);
			}
			else if (response is SFS2XCreateRoomResponse roomResp)
			{
				messageType = SFS2XMessageType.CreateRoomResponse;
				requestId = roomResp.RequestId;
				
				// Write CreateRoomResponse payload: success flag + room data + user data
				payloadWriter.Write(roomResp.Success);
				roomResp.Room.Serialize(payloadWriter);
				roomResp.User.Serialize(payloadWriter);
			}
			else
			{
				throw new ArgumentException($"Unsupported response type: {response.GetType()}");
			}
			
			payload = payloadStream.ToArray();
		}

		// Now build the complete message with SFS2X header
		using var messageStream = new MemoryStream();
		using var messageWriter = new BinaryWriter(messageStream, Encoding.UTF8);
		
		// Write SFS2X header: [Message Type][Request ID][Message Size]
		messageWriter.Write((byte)messageType);
		messageWriter.Write(requestId);
		messageWriter.Write((uint)payload.Length);
		
		// Write payload
		messageWriter.Write(payload);

		var result = messageStream.ToArray();
		
		_logger.LogDebug("Serialized SFS2X response: Type={MessageType}, RequestID={RequestId}, Size={Size} bytes",
			messageType, requestId, result.Length);
			
		return result;
	}

	/// <summary>
	/// Creates a binary error response using proper SFS2X protocol format.
	/// </summary>
	private byte[] CreateBinaryErrorResponse(string errorMessage, ushort requestId = 0)
	{
		// Build payload first
		byte[] payload;
		using (var payloadStream = new MemoryStream())
		using (var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8))
		{
			payloadWriter.Write(false); // Success = false
			
			// Write error message as SFS2X string
			WriteSFS2XString(payloadWriter, errorMessage);
			
			payload = payloadStream.ToArray();
		}

		// Build complete message with SFS2X header
		using var messageStream = new MemoryStream();
		using var messageWriter = new BinaryWriter(messageStream, Encoding.UTF8);
		
		// Write SFS2X header: [Message Type][Request ID][Message Size]
		messageWriter.Write((byte)SFS2XMessageType.ErrorResponse);
		messageWriter.Write(requestId);
		messageWriter.Write((uint)payload.Length);
		
		// Write payload
		messageWriter.Write(payload);

		var result = messageStream.ToArray();
		
		_logger.LogDebug("Created SFS2X error response: RequestID={RequestId}, Error='{ErrorMessage}', Size={Size} bytes",
			requestId, errorMessage, result.Length);
			
		return result;
	}

	/// <summary>
	/// Creates a binary success response using proper SFS2X protocol format.
	/// </summary>
	private byte[] CreateBinarySuccessResponse(ushort requestId, string message)
	{
		// Build payload first
		byte[] payload;
		using (var payloadStream = new MemoryStream())
		using (var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8))
		{
			payloadWriter.Write(true); // Success = true
			
			// Write success message as SFS2X string
			WriteSFS2XString(payloadWriter, message);
			
			payload = payloadStream.ToArray();
		}

		// Build complete message with SFS2X header
		using var messageStream = new MemoryStream();
		using var messageWriter = new BinaryWriter(messageStream, Encoding.UTF8);
		
		// Write SFS2X header: [Message Type][Request ID][Message Size]
		messageWriter.Write((byte)SFS2XMessageType.GenericResponse);
		messageWriter.Write(requestId);
		messageWriter.Write((uint)payload.Length);
		
		// Write payload
		messageWriter.Write(payload);

		var result = messageStream.ToArray();
		
		_logger.LogDebug("Created SFS2X success response: RequestID={RequestId}, Message='{Message}', Size={Size} bytes",
			requestId, message, result.Length);
			
		return result;
	}

	/// <summary>
	/// Writes an SFS2X string to the binary stream.
	/// </summary>
	private void WriteSFS2XString(BinaryWriter writer, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			writer.Write((ushort)0);
		}
		else
		{
			var bytes = Encoding.UTF8.GetBytes(value);
			writer.Write((ushort)bytes.Length);
			writer.Write(bytes);
		}
	}

	/// <summary>
	/// Helper methods (reused from existing processor).
	/// </summary>
	private LobbySettings ParseLobbySettingsFromSFSArray(SFSArray array)
	{
		// TODO: Implement the same 21-element parsing logic from SFS2XMessageProcessor
		return new LobbySettings { Name = "Default Lobby" };
	}

	private CombinedID ParsePlayerIDFromString(string playerIdString)
	{
		return new CombinedID(playerIdString, StorefrontId.Steam, string.Empty, 0);
	}

	private int GenerateP2PPort(string clientIP)
	{
		var hash = clientIP.GetHashCode();
		return 50000 + Math.Abs(hash % 10000);
	}

	private SFSObject CreateSFS2XRoom(RoomInfo room)
	{
		var roomObj = new SFSObject();
		roomObj.PutInt("id", room.RoomId);
		roomObj.PutUtfString("name", room.Name);
		roomObj.PutInt("userCount", room.UserCount);
		roomObj.PutInt("maxUsers", room.MaxUsers);
		return roomObj;
	}

	private SFSObject CreateSFS2XUser(string playerId, int roomId)
	{
		var userObj = new SFSObject();
		userObj.PutUtfString("name", playerId);
		userObj.PutInt("id", roomId);
		return userObj;
	}
}

/// <summary>
/// SFS2X message header structure.
/// </summary>
public class SFS2XMessageHeader
{
	public SFS2XMessageType MessageType { get; set; }
	public ushort RequestId { get; set; }
	public uint MessageSize { get; set; }
}

/// <summary>
/// SFS2X message types for lobby protocol communication.
/// These values are research-based placeholders implemented to match
/// the expected SmartFoxServer 2X binary protocol structure.
/// 
/// Based on decompiled game analysis, the game uses:
/// - LoginRequest("userName", "", "Offworld", params)
/// - CreateRoomRequest(roomSettings, autoJoin: true)
/// - JoinRoomRequest(roomId) or JoinRoomRequest(roomId, password)
/// - ExtensionRequest("registerData", userObject)
/// </summary>
public enum SFS2XMessageType : byte
{
	// Authentication protocol
	LoginRequest = 0x00,        // Client authentication to SFS2X server
	LoginResponse = 0x01,       // Server response with session info
	
	// Room management protocol  
	CreateRoomRequest = 0x10,   // Client requests room creation
	CreateRoomResponse = 0x11,  // Server confirms room creation
	JoinRoomRequest = 0x12,     // Client requests to join room
	JoinRoomResponse = 0x13,    // Server confirms room join
	LeaveRoomRequest = 0x14,    // Client requests to leave room
	
	// Room variables and extensions
	SetRoomVariablesRequest = 0x20,  // Update room variables
	ExtensionRequest = 0x30,         // Custom extension calls (registerData, etc.)
	
	// Error handling
	ErrorResponse = 0xFF,       // Server error response
	GenericResponse = 0x80      // Generic success response
}

/// <summary>
/// SFS2X login response structure.
/// </summary>
public class SFS2XLoginResponse
{
	public bool Success { get; set; }
	public ushort RequestId { get; set; }
	public SFSObject Data { get; set; } = new();
}

/// <summary>
/// SFS2X create room response structure.
/// </summary>
public class SFS2XCreateRoomResponse
{
	public bool Success { get; set; }
	public ushort RequestId { get; set; }
	public SFSObject Room { get; set; } = new();
	public SFSObject User { get; set; } = new();
}