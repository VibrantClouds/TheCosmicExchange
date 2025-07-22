namespace OffworldLobbyServer.Core.Interfaces;

/// <summary>
/// Interface for SFS2X TCP service that handles direct binary protocol connections on port 9933.
/// </summary>
public interface ISFS2XTcpService
{
	/// <summary>
	/// Starts the SFS2X TCP listener on the configured port.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token for service shutdown.</param>
	/// <returns>Task representing the service operation.</returns>
	Task StartAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Stops the SFS2X TCP listener.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token for service shutdown.</param>
	/// <returns>Task representing the service shutdown.</returns>
	Task StopAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Gets the current status of the TCP service.
	/// </summary>
	/// <returns>Service status information.</returns>
	Task<ServiceStatus> GetStatusAsync();
}

/// <summary>
/// Status information for the SFS2X TCP service.
/// </summary>
public class ServiceStatus
{
	/// <summary>
	/// Whether the service is currently running.
	/// </summary>
	public bool IsRunning { get; init; }

	/// <summary>
	/// The port the service is listening on.
	/// </summary>
	public int Port { get; init; }

	/// <summary>
	/// Number of active connections.
	/// </summary>
	public int ActiveConnections { get; init; }

	/// <summary>
	/// Total connections since service start.
	/// </summary>
	public long TotalConnections { get; init; }

	/// <summary>
	/// Service start time.
	/// </summary>
	public DateTime StartTime { get; init; }
}