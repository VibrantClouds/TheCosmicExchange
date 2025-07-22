using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Core.Services;

namespace OffworldLobbyServer.Infrastructure.Services;

/// <summary>
/// TCP service for handling direct SFS2X binary protocol connections on port 9933.
/// This implements the primary SmartFoxServer connection method that games try first.
/// </summary>
public class SFS2XTcpService : ISFS2XTcpService, IDisposable
{
	private readonly ILogger<SFS2XTcpService> _logger;
	private readonly ISessionManager _sessionManager;
	private readonly ServerConfiguration _config;
	private readonly SFS2XMessageProcessor _messageProcessor;
	private readonly SFS2XBinaryMessageProcessor _binaryMessageProcessor;

	private TcpListener? _tcpListener;
	private CancellationTokenSource? _cancellationTokenSource;
	private readonly ConcurrentDictionary<string, SFS2XConnection> _activeConnections = new();
	private DateTime _startTime;
	private long _totalConnections;
	private volatile bool _isRunning;

	public SFS2XTcpService(
		ILogger<SFS2XTcpService> logger,
		ISessionManager sessionManager,
		IOptions<ServerConfiguration> config,
		SFS2XMessageProcessor messageProcessor,
		SFS2XBinaryMessageProcessor binaryMessageProcessor)
	{
		_logger = logger;
		_sessionManager = sessionManager;
		_config = config.Value;
		_messageProcessor = messageProcessor;
		_binaryMessageProcessor = binaryMessageProcessor;
	}

	/// <summary>
	/// Starts the SFS2X TCP listener on port 9933.
	/// </summary>
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		if (_isRunning)
		{
			_logger.LogWarning("SFS2X TCP service is already running");
			return;
		}

		if (!_config.Protocol.EnableSFS2XDirect)
		{
			_logger.LogInformation("SFS2X Direct protocol is disabled in configuration");
			return;
		}

		try
		{
			_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var port = _config.Ports.SFS2XDirect;

			_tcpListener = new TcpListener(IPAddress.Any, port);
			_tcpListener.Start();

			_startTime = DateTime.UtcNow;
			_isRunning = true;

			_logger.LogInformation("SFS2X TCP service started on port {Port}", port);

			// Start accepting connections
			_ = Task.Run(async () => await AcceptConnectionsAsync(_cancellationTokenSource.Token), cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to start SFS2X TCP service");
			throw;
		}

		await Task.CompletedTask;
	}

	/// <summary>
	/// Stops the SFS2X TCP listener.
	/// </summary>
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		if (!_isRunning)
		{
			return;
		}

		_logger.LogInformation("Stopping SFS2X TCP service");

		_isRunning = false;
		_cancellationTokenSource?.Cancel();

		// Stop the listener
		_tcpListener?.Stop();

		// Close all active connections
		var disconnectTasks = _activeConnections.Values.Select(conn => conn.DisconnectAsync());
		await Task.WhenAll(disconnectTasks);

		_activeConnections.Clear();

		_logger.LogInformation("SFS2X TCP service stopped");
	}

	/// <summary>
	/// Gets the current status of the TCP service.
	/// </summary>
	public Task<ServiceStatus> GetStatusAsync()
	{
		var status = new ServiceStatus
		{
			IsRunning = _isRunning,
			Port = _config.Ports.SFS2XDirect,
			ActiveConnections = _activeConnections.Count,
			TotalConnections = Interlocked.Read(ref _totalConnections),
			StartTime = _startTime
		};

		return Task.FromResult(status);
	}

	/// <summary>
	/// Accepts incoming TCP connections and handles them.
	/// </summary>
	private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
	{
		_logger.LogDebug("Starting connection acceptance loop");

		while (!cancellationToken.IsCancellationRequested && _isRunning)
		{
			try
			{
				var tcpClient = await _tcpListener!.AcceptTcpClientAsync();
				Interlocked.Increment(ref _totalConnections);

				// Handle connection asynchronously
				_ = Task.Run(async () => await HandleConnectionAsync(tcpClient, cancellationToken), cancellationToken);
			}
			catch (ObjectDisposedException)
			{
				// Expected when service is being stopped
				break;
			}
			catch (Exception ex)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}

				_logger.LogError(ex, "Error accepting SFS2X TCP connection");
				
				// Brief delay to prevent tight loop on persistent errors
				await Task.Delay(1000, cancellationToken);
			}
		}

		_logger.LogDebug("Connection acceptance loop stopped");
	}

	/// <summary>
	/// Handles an individual TCP connection.
	/// </summary>
	private async Task HandleConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken)
	{
		var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
		var connectionId = Guid.NewGuid().ToString();

		_logger.LogDebug("New SFS2X TCP connection from {Endpoint} (ID: {ConnectionId})", clientEndpoint, connectionId);

		try
		{
			var connection = new SFS2XConnection(connectionId, tcpClient, _logger, _sessionManager, _messageProcessor, _binaryMessageProcessor);
			_activeConnections[connectionId] = connection;

			await connection.ProcessConnectionAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			_logger.LogDebug("SFS2X connection {ConnectionId} cancelled", connectionId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error handling SFS2X connection {ConnectionId} from {Endpoint}", connectionId, clientEndpoint);
		}
		finally
		{
			_activeConnections.TryRemove(connectionId, out _);
			tcpClient?.Close();
			_logger.LogDebug("SFS2X connection {ConnectionId} closed", connectionId);
		}
	}

	public void Dispose()
	{
		_cancellationTokenSource?.Dispose();
		_tcpListener?.Stop();
	}
}

/// <summary>
/// Represents an individual SFS2X TCP connection.
/// </summary>
internal class SFS2XConnection
{
	private readonly string _connectionId;
	private readonly TcpClient _tcpClient;
	private readonly NetworkStream _stream;
	private readonly ILogger _logger;
	private readonly ISessionManager _sessionManager;
	private readonly SFS2XMessageProcessor _messageProcessor;
	private readonly SFS2XBinaryMessageProcessor _binaryMessageProcessor;
	private string? _sessionId;

	public SFS2XConnection(
		string connectionId,
		TcpClient tcpClient,
		ILogger logger,
		ISessionManager sessionManager,
		SFS2XMessageProcessor messageProcessor,
		SFS2XBinaryMessageProcessor binaryMessageProcessor)
	{
		_connectionId = connectionId;
		_tcpClient = tcpClient;
		_stream = tcpClient.GetStream();
		_logger = logger;
		_sessionManager = sessionManager;
		_messageProcessor = messageProcessor;
		_binaryMessageProcessor = binaryMessageProcessor;
	}

	/// <summary>
	/// Processes the SFS2X connection, reading and handling messages.
	/// </summary>
	public async Task ProcessConnectionAsync(CancellationToken cancellationToken)
	{
		var buffer = new byte[4096];
		var clientIP = GetClientIPAddress();

		// Create a session for this direct connection
		var session = await _sessionManager.CreateSessionAsync(clientIP);
		_sessionId = session.SessionId;

		_logger.LogInformation("Created session {SessionId} for direct SFS2X connection {ConnectionId}", _sessionId, _connectionId);

		try
		{
			while (!cancellationToken.IsCancellationRequested && _tcpClient.Connected)
			{
				// Read message length (SFS2X messages typically start with length header)
				var lengthBuffer = new byte[4];
				var bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
				
				if (bytesRead == 0)
				{
					_logger.LogDebug("Client closed connection {ConnectionId}", _connectionId);
					break;
				}

				if (bytesRead < 4)
				{
					_logger.LogWarning("Incomplete length header from connection {ConnectionId}", _connectionId);
					break;
				}

				// Parse message length (big-endian)
				var messageLength = BitConverter.ToInt32(lengthBuffer.Reverse().ToArray(), 0);
				
				if (messageLength <= 0 || messageLength > 65536) // Reasonable size limits
				{
					_logger.LogWarning("Invalid message length {Length} from connection {ConnectionId}", messageLength, _connectionId);
					break;
				}

				// Read the actual message
				var messageBuffer = new byte[messageLength];
				var totalBytesRead = 0;
				
				while (totalBytesRead < messageLength)
				{
					bytesRead = await _stream.ReadAsync(
						messageBuffer, totalBytesRead, 
						messageLength - totalBytesRead, 
						cancellationToken);
					
					if (bytesRead == 0)
					{
						_logger.LogWarning("Unexpected end of stream while reading message from connection {ConnectionId}", _connectionId);
						return;
					}
					
					totalBytesRead += bytesRead;
				}

				_logger.LogDebug("Received SFS2X message: {Length} bytes from connection {ConnectionId}", messageLength, _connectionId);

				// Process the SFS2X binary message
				await ProcessSFS2XBinaryMessage(messageBuffer, cancellationToken);
			}
		}
		catch (IOException ex)
		{
			_logger.LogDebug(ex, "IO error on connection {ConnectionId} - client likely disconnected", _connectionId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing connection {ConnectionId}", _connectionId);
		}
	}

	/// <summary>
	/// Processes a direct SFS2X binary message (native binary format).
	/// </summary>
	private async Task ProcessSFS2XBinaryMessage(byte[] messageData, CancellationToken cancellationToken)
	{
		try
		{
			// Process with native binary SFS2X processor
			var responseBytes = await _binaryMessageProcessor.ProcessBinaryMessage(_sessionId!, messageData);
			
			if (responseBytes != null)
			{
				await SendSFS2XResponse(responseBytes, cancellationToken);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing SFS2X binary message for connection {ConnectionId}", _connectionId);
		}
	}

	/// <summary>
	/// Sends an SFS2X binary response to the client.
	/// </summary>
	private async Task SendSFS2XResponse(byte[] responseData, CancellationToken cancellationToken)
	{
		try
		{
			// Send length header (big-endian) followed by message
			var lengthBytes = BitConverter.GetBytes(responseData.Length).Reverse().ToArray();
			
			await _stream.WriteAsync(lengthBytes, 0, 4, cancellationToken);
			await _stream.WriteAsync(responseData, 0, responseData.Length, cancellationToken);
			await _stream.FlushAsync(cancellationToken);

			_logger.LogDebug("Sent SFS2X response: {Length} bytes to connection {ConnectionId}", responseData.Length, _connectionId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending SFS2X response to connection {ConnectionId}", _connectionId);
		}
	}

	/// <summary>
	/// Gets the client IP address from the TCP connection.
	/// </summary>
	private string GetClientIPAddress()
	{
		try
		{
			if (_tcpClient.Client.RemoteEndPoint is IPEndPoint ipEndPoint)
			{
				return ipEndPoint.Address.ToString();
			}
		}
		catch
		{
			// Ignore exceptions during IP extraction
		}
		
		return "unknown";
	}

	/// <summary>
	/// Disconnects the TCP connection.
	/// </summary>
	public async Task DisconnectAsync()
	{
		try
		{
			if (_sessionId != null)
			{
				await _sessionManager.DisconnectSessionAsync(_sessionId);
			}

			_stream?.Close();
			_tcpClient?.Close();
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Error during disconnect of connection {ConnectionId}", _connectionId);
		}
	}
}