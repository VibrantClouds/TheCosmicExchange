using System.Text;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Constants;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Models;
using OffworldLobbyServer.Shared.SFS2X;
using OffworldLobbyServer.Shared.SFS2X.Protocol;

namespace OffworldLobbyServer.Core.Services;

/// <summary>
/// Unified SFS2X protocol processor implementing confirmed specifications.
/// Handles both BlueBox HTTP transport and direct TCP connections with single protocol logic.
/// Replaces both SFS2XMessageProcessor and SFS2XBinaryMessageProcessor.
/// </summary>
public class SFS2XProtocolProcessor : ISFS2XProtocolProcessor
{
	private readonly ISessionManager _sessionManager;
	private readonly IRoomManager _roomManager;
	private readonly ILogger<SFS2XProtocolProcessor> _logger;

	public SFS2XProtocolProcessor(
		ISessionManager sessionManager,
		IRoomManager roomManager,
		ILogger<SFS2XProtocolProcessor> logger)
	{
		_sessionManager = sessionManager;
		_roomManager = roomManager;
		_logger = logger;
	}

	/// <summary>
	/// Processes a base64-encoded SFS2X message from BlueBox HTTP transport.
	/// </summary>
	public async Task<string?> ProcessBlueBoxMessage(string sessionId, string base64Data)
	{
		try
		{
			_logger.LogDebug("=== BLUEBOX INCOMING MESSAGE ===");
			_logger.LogDebug("Session: {SessionId}, Base64 Length: {Length}", sessionId, base64Data.Length);
			_logger.LogDebug("Base64 Data: {Data}", base64Data);
			
			var messageBytes = Convert.FromBase64String(base64Data);
			_logger.LogDebug("Decoded to {Length} bytes", messageBytes.Length);

			var responseBytes = await ProcessBinaryMessage(sessionId, messageBytes);
			
			if (responseBytes != null)
			{
				var base64Response = Convert.ToBase64String(responseBytes);
				_logger.LogDebug("=== BLUEBOX OUTGOING RESPONSE ===");
				_logger.LogDebug("Response Base64 ({Length} chars): {Data}", base64Response.Length, base64Response);
				return base64Response;
			}
			
			return null;
		}
		catch (FormatException ex)
		{
			_logger.LogWarning(ex, "Invalid base64 data in BlueBox message for session {SessionId}", sessionId);
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing BlueBox message for session {SessionId}", sessionId);
			return null;
		}
	}

	/// <summary>
	/// Processes raw binary SFS2X message data according to confirmed specification.
	/// Format: [Controller:1][Action:1][Length:4][SFSObject payload]
	/// </summary>
	public async Task<byte[]?> ProcessBinaryMessage(string sessionId, byte[] messageData)
	{
		try
		{
			_logger.LogDebug("Processing binary SFS2X message: {Length} bytes", messageData.Length);
			
			// Log detailed protocol analysis
			LogProtocolAnalysis(messageData);

			// Parse message using confirmed format
			var message = SFS2XMessage.FromBytes(messageData);
			var header = message.Header;
			
			_logger.LogInformation("SFS2X Message - Protocol: 0x{Protocol:X2}, Format: {Format}, Controller: 0x{Controller:X2}, Action: 0x{Action:X2}, PayloadSize: {PayloadSize}",
				header.ProtocolHeader, header.IsCompactFormat ? "Compact" : "Standard", header.Controller, header.Action, header.Length);
			
			// Log compact format details if applicable
			if (header.IsCompactFormat && message.Payload.ContainsKey("c"))
			{
				object? cmdValue = null;
				try 
				{
					// Try to get command value as different types
					cmdValue = message.Payload.GetInt("c");
				}
				catch 
				{
					try { cmdValue = message.Payload.GetUtfString("c"); }
					catch { cmdValue = "unknown"; }
				}
				
				_logger.LogDebug("Compact format wrapper - Command: {Command}, Has Parameters: {HasParams}", 
					cmdValue ?? "null", message.Payload.ContainsKey("p"));
			}

			// Route message based on controller and action
			return (header.Controller, header.Action) switch
			{
				// System Controller messages
				(SFS2XControllers.SYSTEM, SFS2XSystemActions.HANDSHAKE) => await HandleHandshakeRequest(sessionId, message.Payload),
				(SFS2XControllers.SYSTEM, SFS2XSystemActions.LOGIN_REQUEST) => await HandleLoginRequest(sessionId, message.Payload),
				(SFS2XControllers.SYSTEM, SFS2XSystemActions.JOIN_ROOM) => await HandleJoinRoomRequest(sessionId, message.Payload),

				// Extension Controller messages  
				(SFS2XControllers.EXTENSION, SFS2XExtensionActions.EXTENSION_REQUEST) => await HandleExtensionRequest(sessionId, message.Payload),

				// Unknown message types
				_ => await HandleUnknownMessage(sessionId, header.Controller, header.Action, message.Payload)
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing binary SFS2X message for session {SessionId}", sessionId);
			return CreateErrorResponse("Message processing failed");
		}
	}

	/// <summary>
	/// Parses SFS2X message header according to confirmed specification.
	/// </summary>
	public SFS2XMessageHeader ParseMessageHeader(byte[] messageData)
	{
		return SFS2XMessageHeader.FromBytes(messageData);
	}

	/// <summary>
	/// Creates a properly formatted SFS2X response message.
	/// </summary>
	public byte[] CreateResponseMessage(byte controller, byte action, SFSObject payload)
	{
		var message = new SFS2XMessage(controller, action, payload);
		var responseBytes = message.ToBytes();
		
		// Log outgoing message for debugging
		_logger.LogInformation("=== SFS2X RESPONSE MESSAGE ===");
		_logger.LogInformation("Controller: 0x{Controller:X2}, Action: 0x{Action:X2}", controller, action);
		_logger.LogInformation("Response ({Length} bytes): {Hex}", responseBytes.Length, Convert.ToHexString(responseBytes));
		
		return responseBytes;
	}

	/// <summary>
	/// Handles System Controller handshake requests.
	/// </summary>
	private async Task<byte[]> HandleHandshakeRequest(string sessionId, SFSObject payload)
	{
		_logger.LogInformation("Processing handshake request for session {SessionId}", sessionId);

		// Extract client info from payload
		var clientApi = payload.ContainsKey("api") ? payload.GetUtfString("api") : "unknown";
		var clientType = payload.ContainsKey("cl") ? payload.GetUtfString("cl") : "unknown";

		_logger.LogDebug("Handshake - Client API: {Api}, Type: {Type}", clientApi, clientType);

		// Create handshake response
		var responsePayload = new SFSObject();
		responsePayload.PutUtfString("serverVersion", "2.0.0");
		responsePayload.PutBool("success", true);

		return CreateResponseMessage(SFS2XControllers.SYSTEM, SFS2XSystemActions.HANDSHAKE, responsePayload);
	}

	/// <summary>
	/// Handles System Controller login requests according to confirmed specification.
	/// </summary>
	private async Task<byte[]> HandleLoginRequest(string sessionId, SFSObject payload)
	{
		try
		{
			_logger.LogInformation("Processing login request for session {SessionId}", sessionId);

			// Parse login request using confirmed format
			var loginReq = LoginRequest.FromSFSObject(payload);
			_logger.LogDebug("Login request - Zone: {Zone}, User: {User}", loginReq.ZoneName, loginReq.UserName);

			// Validate zone
			if (loginReq.ZoneName != "BasicExamples" && loginReq.ZoneName != "Offworld")
			{
				_logger.LogWarning("Invalid zone '{Zone}' in login request", loginReq.ZoneName);
				return CreateLoginErrorResponse("Invalid zone");
			}

			// Get session for client IP info
			var session = await _sessionManager.GetSessionAsync(sessionId);
			if (session == null)
			{
				_logger.LogWarning("Session not found for login request: {SessionId}", sessionId);
				return CreateLoginErrorResponse("Session not found");
			}

			// Parse player ID
			var playerId = ParsePlayerIDFromString(loginReq.UserName);
			await _sessionManager.SetSessionPlayerAsync(sessionId, playerId);

			// Create login response with confirmed structure
			var loginResponse = new LoginResponse
			{
				User = new UserInfo
				{
					Id = 42, // Fixed user ID for testing
					Name = loginReq.UserName,
					PrivilegeId = 1,
					IsItMe = true
				},
				Data = new SFSObject(), // Empty data object as per specification
				Zone = loginReq.ZoneName
			};

			_logger.LogInformation("Login successful for {User} in zone {Zone}", loginReq.UserName, loginReq.ZoneName);

			return CreateResponseMessage(SFS2XControllers.SYSTEM, SFS2XSystemActions.LOGIN_REQUEST, loginResponse.ToSFSObject());
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling login request for session {SessionId}", sessionId);
			return CreateLoginErrorResponse("Login processing failed");
		}
	}

	/// <summary>
	/// Handles Extension Controller requests, particularly "registerData".
	/// </summary>
	private async Task<byte[]> HandleExtensionRequest(string sessionId, SFSObject payload)
	{
		try
		{
			_logger.LogInformation("🎉 Processing extension request for session {SessionId}", sessionId);

			// Parse extension request using confirmed format
			var extReq = ExtensionRequest.FromSFSObject(payload);
			_logger.LogInformation("Extension request - Command: {Command}, Room: {Room}", extReq.Command, extReq.RoomId);

			// Handle specific extension commands
			return extReq.Command switch
			{
				"registerData" => await HandleRegisterDataExtension(sessionId, extReq),
				"gameMove" => await HandleGameMoveExtension(sessionId, extReq),
				_ => await HandleUnknownExtension(sessionId, extReq)
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling extension request for session {SessionId}", sessionId);
			return CreateExtensionErrorResponse("Extension processing failed");
		}
	}

	/// <summary>
	/// Handles the critical "registerData" extension request.
	/// </summary>
	private async Task<byte[]> HandleRegisterDataExtension(string sessionId, ExtensionRequest request)
	{
		_logger.LogInformation("Processing registerData extension for session {SessionId}", sessionId);

		// Create response parameters with game data
		var responseParams = new SFSObject();
		responseParams.PutUtfString("status", "registered");
		responseParams.PutInt("playerId", 42);
		responseParams.PutUtfString("serverMessage", "Registration successful");

		// Create extension response
		var extResponse = ExtensionResponse.FromRequest(request, responseParams);

		return CreateResponseMessage(SFS2XControllers.EXTENSION, SFS2XExtensionActions.EXTENSION_RESPONSE, extResponse.ToSFSObject());
	}

	/// <summary>
	/// Handles game move extension requests.
	/// </summary>
	private async Task<byte[]> HandleGameMoveExtension(string sessionId, ExtensionRequest request)
	{
		_logger.LogDebug("Processing gameMove extension for session {SessionId}", sessionId);

		var responseParams = new SFSObject();
		responseParams.PutUtfString("result", "move_processed");

		var extResponse = ExtensionResponse.FromRequest(request, responseParams);
		return CreateResponseMessage(SFS2XControllers.EXTENSION, SFS2XExtensionActions.EXTENSION_RESPONSE, extResponse.ToSFSObject());
	}

	/// <summary>
	/// Handles join room requests.
	/// </summary>
	private async Task<byte[]> HandleJoinRoomRequest(string sessionId, SFSObject payload)
	{
		_logger.LogInformation("Processing join room request for session {SessionId}", sessionId);

		// TODO: Implement proper room joining logic
		var responsePayload = new SFSObject();
		responsePayload.PutBool("success", true);
		responsePayload.PutUtfString("message", "Room join successful");

		return CreateResponseMessage(SFS2XControllers.SYSTEM, SFS2XSystemActions.JOIN_ROOM, responsePayload);
	}

	/// <summary>
	/// Handles unknown extension commands.
	/// </summary>
	private async Task<byte[]> HandleUnknownExtension(string sessionId, ExtensionRequest request)
	{
		_logger.LogWarning("Unknown extension command '{Command}' for session {SessionId}", request.Command, sessionId);

		var responseParams = new SFSObject();
		responseParams.PutUtfString("error", $"Unknown command: {request.Command}");

		var extResponse = ExtensionResponse.FromRequest(request, responseParams);
		return CreateResponseMessage(SFS2XControllers.EXTENSION, SFS2XExtensionActions.EXTENSION_RESPONSE, extResponse.ToSFSObject());
	}

	/// <summary>
	/// Handles unknown message types.
	/// </summary>
	private async Task<byte[]?> HandleUnknownMessage(string sessionId, byte controller, byte action, SFSObject payload)
	{
		_logger.LogWarning("Unknown message - Controller: 0x{Controller:X2}, Action: 0x{Action:X2} for session {SessionId}",
			controller, action, sessionId);
		return null; // No response for unknown messages
	}

	/// <summary>
	/// Creates a login error response.
	/// </summary>
	private byte[] CreateLoginErrorResponse(string errorMessage)
	{
		var errorPayload = new SFSObject();
		errorPayload.PutUtfString("error", errorMessage);
		errorPayload.PutBool("success", false);

		return CreateResponseMessage(SFS2XControllers.SYSTEM, SFS2XSystemActions.LOGIN_ERROR, errorPayload);
	}

	/// <summary>
	/// Creates an extension error response.
	/// </summary>
	private byte[] CreateExtensionErrorResponse(string errorMessage)
	{
		var errorPayload = new SFSObject();
		errorPayload.PutUtfString("error", errorMessage);

		return CreateResponseMessage(SFS2XControllers.EXTENSION, SFS2XExtensionActions.EXTENSION_RESPONSE, errorPayload);
	}

	/// <summary>
	/// Creates a generic error response.
	/// </summary>
	private byte[] CreateErrorResponse(string errorMessage)
	{
		var errorPayload = new SFSObject();
		errorPayload.PutUtfString("error", errorMessage);

		// Use system controller with handshake action for generic errors
		return CreateResponseMessage(SFS2XControllers.SYSTEM, SFS2XSystemActions.HANDSHAKE, errorPayload);
	}

	/// <summary>
	/// Logs detailed protocol analysis for debugging.
	/// </summary>
	private void LogProtocolAnalysis(byte[] messageData)
	{
		_logger.LogInformation("=== SFS2X PROTOCOL ANALYSIS ===");
		_logger.LogInformation("Raw message ({Length} bytes): {Hex}", messageData.Length, Convert.ToHexString(messageData));

		if (messageData.Length >= 3)
		{
			_logger.LogInformation("Header analysis:");
			_logger.LogInformation("  Protocol Header: 0x{Protocol:X2} ({Type})", 
				messageData[0], 
				messageData[0] == 0x80 ? "System" : messageData[0] == 0x90 ? "Extension" : "Unknown");
			
			// Detect format: Standard format has two consecutive 0x00 bytes
			bool isStandardFormat = messageData.Length >= 3 && messageData[1] == 0x00 && messageData[2] == 0x00;
			_logger.LogInformation("  Format: {Format}", isStandardFormat ? "Standard (6-byte header)" : "Compact (3-byte header)");
			
			if (!isStandardFormat)
			{
				// Compact format
				if (messageData.Length >= 3)
				{
					var length = (uint)((messageData[1] << 8) | messageData[2]);
					_logger.LogInformation("  Payload Length: {Length} bytes", length);
					
					if (messageData.Length > 3)
					{
						var payloadPreview = messageData.Length > 13 ? messageData[3..13] : messageData[3..];
						_logger.LogInformation("  Payload Preview: {Hex}", Convert.ToHexString(payloadPreview));
					}
				}
			}
			else
			{
				// Standard format
				if (messageData.Length >= 6)
				{
					var length = (uint)messageData[3];
					_logger.LogInformation("  Payload Length: {Length} bytes", length);
					_logger.LogInformation("  Controller: 0x{Controller:X2} ({Controller})", messageData[4], messageData[4]);
					_logger.LogInformation("  Action: 0x{Action:X2} ({Action})", messageData[5], messageData[5]);

					if (messageData.Length > 6)
					{
						var payloadPreview = messageData.Length > 16 ? messageData[6..16] : messageData[6..];
						_logger.LogInformation("  Payload Preview: {Hex}", Convert.ToHexString(payloadPreview));
					}
				}
			}
		}
		else
		{
			_logger.LogWarning("Message too short for header analysis (need at least 3 bytes)");
		}
	}

	/// <summary>
	/// Parses player ID from username string.
	/// </summary>
	private CombinedID ParsePlayerIDFromString(string playerIdString)
	{
		// TODO: Implement proper player ID parsing based on game's format
		return new CombinedID(playerIdString, Shared.Enums.StorefrontId.Steam, string.Empty, 0);
	}
}