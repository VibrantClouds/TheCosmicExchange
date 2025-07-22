using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;

namespace OffworldLobbyServer.Infrastructure.Services;

/// <summary>
/// Background service that periodically cleans up expired sessions
/// Runs every 5 minutes to remove sessions that have timed out.
/// </summary>
public class SessionCleanupService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<SessionCleanupService> _logger;
	private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
	private readonly int _sessionTimeoutMinutes = 30;

	public SessionCleanupService(
		IServiceProvider serviceProvider,
		ILogger<SessionCleanupService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
		_logger.LogInformation(
			"SessionCleanupService initialized (interval: {Interval}, timeout: {Timeout}min)",
			_cleanupInterval, _sessionTimeoutMinutes);
	}

	/// <inheritdoc/>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("SessionCleanupService starting");

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
				_logger.LogError(ex, "Error during session cleanup cycle");

				// Wait a shorter interval before retrying after an error
				try
				{
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}

		_logger.LogInformation("SessionCleanupService stopped");
	}

	/// <summary>
	/// Performs the actual session cleanup using a scoped service.
	/// </summary>
	private async Task PerformCleanupAsync()
	{
		using var scope = _serviceProvider.CreateScope();
		var sessionManager = scope.ServiceProvider.GetRequiredService<ISessionManager>();

		try
		{
			var cleanedUpCount = await sessionManager.CleanupExpiredSessionsAsync(_sessionTimeoutMinutes);

			if (cleanedUpCount > 0)
			{
				_logger.LogInformation(
					"Session cleanup completed: {Count} expired sessions removed",
					cleanedUpCount);
			}
			else
			{
				_logger.LogTrace("Session cleanup completed: no expired sessions found");
			}

			// Log current session statistics
			var totalSessions = await sessionManager.GetSessionCountAsync();
			_logger.LogTrace("Current active sessions: {Count}", totalSessions);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to perform session cleanup");
			throw;
		}
	}

	/// <inheritdoc/>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("SessionCleanupService stopping...");
		await base.StopAsync(cancellationToken);
		_logger.LogInformation("SessionCleanupService stopped");
	}
}
