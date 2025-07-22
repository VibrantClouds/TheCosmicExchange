using System.Web;
using Microsoft.AspNetCore.Mvc;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Services;

namespace OffworldLobbyServer.Api.Controllers;

/// <summary>
/// BlueBox HTTP protocol handler
/// Implements the exact BlueBox servlet interface used by the game
/// Endpoint: POST /BlueBox/BlueBox.do.
/// </summary>
/// <param name="sessionManager">Session management service.</param>
/// <param name="roomManager">Room management service.</param>
/// <param name="sfs2xProcessor">SFS2X message processor.</param>
/// <param name="logger">Logger instance.</param>
[ApiController]
[Route("[controller]")]
public class BlueBoxController(
	ISessionManager sessionManager,
	IRoomManager roomManager,
	SFS2XMessageProcessor sfs2xProcessor,
	ILogger<BlueBoxController> logger): ControllerBase
{
	private readonly ISessionManager _sessionManager = sessionManager;
	private readonly SFS2XMessageProcessor _sfs2xProcessor = sfs2xProcessor;
	private readonly ILogger<BlueBoxController> _logger = logger;

	/// <summary>
	/// Main BlueBox servlet endpoint
	/// Handles all BlueBox protocol commands: connect|poll|data|disconnect.
	/// </summary>
	/// <returns>Plain text response.</returns>
	[HttpPost("BlueBox.do")]
	[Consumes("application/x-www-form-urlencoded")]
	[Produces("text/plain")]
	public async Task<IActionResult> BlueBox()
	{
		try
		{
			// Read the raw form data
			var form = await Request.ReadFormAsync();

			// Get the sfsHttp parameter
			if (!form.TryGetValue("sfsHttp", out var encodedCommand))
			{
				_logger.LogWarning("BlueBox request missing sfsHttp parameter");
				return BadRequest("Missing sfsHttp parameter");
			}

			// Remove null terminator if present
			var commandString = encodedCommand.ToString();
			if (commandString.EndsWith('\0'))
			{
				commandString = commandString.TrimEnd('\0');
			}

			_logger.LogDebug("BlueBox command received: {Command}", commandString);

			// URL decode the command
			var decodedCommand = HttpUtility.UrlDecode(commandString);

			// Parse pipe-separated command: sessionId|command|data
			var parts = decodedCommand.Split('|', 3);
			if (parts.Length < 2)
			{
				_logger.LogWarning("Invalid BlueBox command format: {Command}", decodedCommand);
				return Ok("err01|Invalid command format");
			}

			var sessionId = parts[0];
			var command = parts[1];
			var data = parts.Length > 2 ? parts[2] : "null";

			// Get client IP for logging and session creation
			var clientIP = GetClientIPAddress();

			// Process the command
			var response = await ProcessBlueBoxCommand(sessionId, command, data, clientIP);

			_logger.LogDebug("BlueBox response: {Response}", response);

			// Return plain text response
			return Content(response, "text/plain");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing BlueBox request");
			return Ok("err01|Internal server error");
		}
	}

	/// <summary>
	/// Processes individual BlueBox commands.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="command">Command name.</param>
	/// <param name="data">Command data.</param>
	/// <param name="clientIP">Client IP address.</param>
	/// <returns>Response string.</returns>
	private async Task<string> ProcessBlueBoxCommand(string sessionId, string command, string data, string clientIP)
	{
		switch (command.ToLowerInvariant())
		{
			case "connect":
				return await HandleConnect(clientIP);

			case "poll":
				return await HandlePoll(sessionId);

			case "data":
				return await HandleData(sessionId, data);

			case "disconnect":
				return await HandleDisconnect(sessionId);

			default:
				_logger.LogWarning("Unknown BlueBox command: {Command}", command);
				return "err01|Unknown command";
		}
	}

	/// <summary>
	/// Handles connect command - creates new session
	/// Request: null|connect|null
	/// Response: connect|SESS_1234567890ABCDEF.
	/// </summary>
	private async Task<string> HandleConnect(string clientIP)
	{
		try
		{
			var session = await _sessionManager.CreateSessionAsync(clientIP);

			_logger.LogInformation(
				"New BlueBox session created: {SessionId} from {ClientIP}",
				session.SessionId, clientIP);

			return $"connect|{session.SessionId}";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating BlueBox session");
			return "err01|Failed to create session";
		}
	}

	/// <summary>
	/// Handles poll command - long-polling for messages
	/// Request: SESS_123|poll|null
	/// Response: poll|{base64_data} OR poll|null.
	/// </summary>
	private async Task<string> HandlePoll(string sessionId)
	{
		try
		{
			// Validate session exists
			if (!await _sessionManager.UpdateSessionActivityAsync(sessionId))
			{
				_logger.LogWarning("Poll request for invalid session: {SessionId}", sessionId);
				return "err01|Invalid http session !";
			}

			// Get next queued message
			var message = await _sessionManager.PollMessageAsync(sessionId);

			if (message != null)
			{
				_logger.LogDebug("Returning queued message for session {SessionId}", sessionId);
				return $"poll|{message}";
			}
			else
			{
				// No messages available
				return "poll|null";
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling poll for session {SessionId}", sessionId);
			return "err01|Poll error";
		}
	}

	/// <summary>
	/// Handles data command - client sending data to server
	/// Request: SESS_123|data|{base64_encoded_data}
	/// Response: data|null (acknowledgment).
	/// </summary>
	private async Task<string> HandleData(string sessionId, string data)
	{
		try
		{
			// Validate session exists
			if (!await _sessionManager.UpdateSessionActivityAsync(sessionId))
			{
				_logger.LogWarning("Data request for invalid session: {SessionId}", sessionId);
				return "err01|Invalid http session !";
			}

			if (data == "null" || string.IsNullOrEmpty(data))
			{
				_logger.LogDebug("Empty data received from session {SessionId}", sessionId);
				return "data|null";
			}

			// Process SFS2X message data
			_logger.LogDebug(
				"Processing SFS2X data from session {SessionId}: {DataLength} chars",
				sessionId, data.Length);

			var response = await _sfs2xProcessor.ProcessSFS2XMessage(sessionId, data);

			if (response != null)
			{
				// Queue the response for polling
				await _sessionManager.QueueMessageAsync(sessionId, response);
				_logger.LogDebug("Response queued for session {SessionId}", sessionId);
			}

			return "data|null"; // Always acknowledge with null
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling data for session {SessionId}", sessionId);
			return "err01|Data error";
		}
	}

	/// <summary>
	/// Handles disconnect command - closes session
	/// Request: SESS_123|disconnect|null
	/// Response: disconnect|null.
	/// </summary>
	private async Task<string> HandleDisconnect(string sessionId)
	{
		try
		{
			var disconnected = await _sessionManager.DisconnectSessionAsync(sessionId);

			if (disconnected)
			{
				_logger.LogInformation("Session disconnected: {SessionId}", sessionId);
			}
			else
			{
				_logger.LogWarning("Disconnect request for unknown session: {SessionId}", sessionId);
			}

			return "disconnect|null";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling disconnect for session {SessionId}", sessionId);
			return "err01|Disconnect error";
		}
	}

	/// <summary>
	/// Gets the client IP address from the request.
	/// </summary>
	/// <returns>Client IP address.</returns>
	private string GetClientIPAddress()
	{
		// Check for X-Forwarded-For header (proxy scenarios)
		if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
		{
			var ip = forwardedFor.ToString().Split(',')[0].Trim();
			if (!string.IsNullOrEmpty(ip))
			{
				return ip;
			}
		}

		// Check for X-Real-IP header
		if (Request.Headers.TryGetValue("X-Real-IP", out var realIP))
		{
			var ip = realIP.ToString();
			if (!string.IsNullOrEmpty(ip))
			{
				return ip;
			}
		}

		// Fall back to connection remote IP
		return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
	}
}

/// <summary>
/// Middleware to add proper BlueBox protocol headers
/// Ensures HTTP/1.0 compatibility and proper content types.
/// </summary>
/// <param name="next">Next middleware in pipeline.</param>
/// <param name="logger">Logger instance.</param>
public class BlueBoxMiddleware(RequestDelegate next, ILogger<BlueBoxMiddleware> logger)
{
	private readonly RequestDelegate _next = next;
	private readonly ILogger<BlueBoxMiddleware> _logger = logger;

	public async Task InvokeAsync(HttpContext context)
	{
		// Log all BlueBox requests
		if (context.Request.Path.StartsWithSegments("/BlueBox"))
		{
			_logger.LogDebug(
				"BlueBox request: {Method} {Path} from {IP}",
				context.Request.Method,
				context.Request.Path,
				context.Connection.RemoteIpAddress);
		}

		await _next(context);

		// Set proper response headers for BlueBox protocol
		if (context.Request.Path.StartsWithSegments("/BlueBox"))
		{
			// Ensure content type is text/plain
			if (!context.Response.ContentType?.StartsWith("text/plain") == true)
			{
				context.Response.ContentType = "text/plain";
			}
		}
	}
}
