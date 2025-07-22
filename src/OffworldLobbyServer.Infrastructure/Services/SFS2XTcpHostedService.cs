using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;

namespace OffworldLobbyServer.Infrastructure.Services;

/// <summary>
/// Hosted service that manages the lifecycle of the SFS2X TCP service.
/// This runs alongside the HTTP server to provide dual-protocol support.
/// </summary>
public class SFS2XTcpHostedService : BackgroundService
{
	private readonly ISFS2XTcpService _tcpService;
	private readonly ILogger<SFS2XTcpHostedService> _logger;

	public SFS2XTcpHostedService(
		ISFS2XTcpService tcpService,
		ILogger<SFS2XTcpHostedService> logger)
	{
		_tcpService = tcpService;
		_logger = logger;
	}

	/// <summary>
	/// Starts the SFS2X TCP service when the host starts.
	/// </summary>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Starting SFS2X TCP hosted service");

		try
		{
			await _tcpService.StartAsync(stoppingToken);

			// Keep the service running until cancellation is requested
			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(5000, stoppingToken); // Check every 5 seconds
				
				// Optionally log service status
				var status = await _tcpService.GetStatusAsync();
				if (status.IsRunning)
				{
					_logger.LogDebug(
						"SFS2X TCP service running: {ActiveConnections} active, {TotalConnections} total connections",
						status.ActiveConnections, status.TotalConnections);
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("SFS2X TCP hosted service is stopping");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in SFS2X TCP hosted service");
			throw;
		}
	}

	/// <summary>
	/// Stops the SFS2X TCP service when the host stops.
	/// </summary>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Stopping SFS2X TCP hosted service");
		
		await _tcpService.StopAsync(cancellationToken);
		await base.StopAsync(cancellationToken);
		
		_logger.LogInformation("SFS2X TCP hosted service stopped");
	}
}