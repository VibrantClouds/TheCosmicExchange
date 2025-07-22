using Microsoft.Extensions.Options;
using OffworldLobbyServer.Core.Models;

namespace OffworldLobbyServer.Api.Middleware;

/// <summary>
/// Middleware that detects and logs protocol information for incoming requests.
/// Helps distinguish between BlueBox HTTP and direct SFS2X TCP connections.
/// </summary>
public class ProtocolDetectionMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ProtocolDetectionMiddleware> _logger;
	private readonly ServerConfiguration _config;

	public ProtocolDetectionMiddleware(
		RequestDelegate next,
		ILogger<ProtocolDetectionMiddleware> logger,
		IOptions<ServerConfiguration> config)
	{
		_next = next;
		_logger = logger;
		_config = config.Value;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Add protocol detection headers to the context
		var localPort = context.Connection.LocalPort;
		var protocol = DetermineProtocol(localPort, context.Request.Path);

		context.Items["Protocol"] = protocol;
		context.Items["ProtocolPort"] = localPort;

		// Log protocol usage for monitoring
		if (protocol == "BlueBoxHttp" && context.Request.Path.StartsWithSegments("/BlueBox"))
		{
			_logger.LogDebug(
				"BlueBox HTTP request: {Method} {Path} from {IP}:{Port}",
				context.Request.Method,
				context.Request.Path,
				context.Connection.RemoteIpAddress,
				context.Connection.RemotePort);
		}
		else if (protocol == "Http" && !context.Request.Path.StartsWithSegments("/BlueBox"))
		{
			_logger.LogDebug(
				"Standard HTTP request: {Method} {Path} from {IP}:{Port}",
				context.Request.Method,
				context.Request.Path,
				context.Connection.RemoteIpAddress,
				context.Connection.RemotePort);
		}

		await _next(context);
	}

	/// <summary>
	/// Determines the protocol type based on port and request path.
	/// </summary>
	private string DetermineProtocol(int port, PathString path)
	{
		if (port == _config.Ports.BlueBoxHttp && path.StartsWithSegments("/BlueBox"))
		{
			return "BlueBoxHttp";
		}

		if (port == _config.Ports.BlueBoxHttp)
		{
			return "Http";
		}

		// Note: SFS2XDirect connections are handled by the TCP service, not HTTP middleware
		return "Unknown";
	}
}