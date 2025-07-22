using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;

namespace OffworldLobbyServer.Infrastructure.Services;

/// <summary>
/// Background service that periodically cleans up abandoned rooms
/// Runs every 10 minutes to remove rooms that are empty or abandoned.
/// </summary>
public class RoomCleanupService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<RoomCleanupService> _logger;
	private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);
	private readonly int _roomTimeoutMinutes = 60;

	public RoomCleanupService(
		IServiceProvider serviceProvider,
		ILogger<RoomCleanupService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
		_logger.LogInformation(
			"RoomCleanupService initialized (interval: {Interval}, timeout: {Timeout}min)",
			_cleanupInterval, _roomTimeoutMinutes);
	}

	/// <inheritdoc/>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("RoomCleanupService starting");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(_cleanupInterval, stoppingToken);

				if (stoppingToken.IsCancellationRequested)
				{
					break;
				}

				await PerformCleanupAsync();
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation token is triggered
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during room cleanup cycle");

				// Wait a shorter interval before retrying after an error
				try
				{
					await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}

		_logger.LogInformation("RoomCleanupService stopped");
	}

	/// <summary>
	/// Performs the actual room cleanup using a scoped service.
	/// </summary>
	private async Task PerformCleanupAsync()
	{
		using var scope = _serviceProvider.CreateScope();
		var roomManager = scope.ServiceProvider.GetRequiredService<IRoomManager>();

		try
		{
			var cleanedUpCount = await roomManager.CleanupAbandonedRoomsAsync(_roomTimeoutMinutes);

			if (cleanedUpCount > 0)
			{
				_logger.LogInformation(
					"Room cleanup completed: {Count} abandoned rooms removed",
					cleanedUpCount);
			}
			else
			{
				_logger.LogTrace("Room cleanup completed: no abandoned rooms found");
			}

			// Log current room statistics
			var totalRooms = await roomManager.GetRoomCountAsync("lobbies");
			_logger.LogTrace("Current active rooms: {Count}", totalRooms);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to perform room cleanup");
			throw;
		}
	}

	/// <inheritdoc/>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("RoomCleanupService stopping...");
		await base.StopAsync(cancellationToken);
		_logger.LogInformation("RoomCleanupService stopped");
	}
}
