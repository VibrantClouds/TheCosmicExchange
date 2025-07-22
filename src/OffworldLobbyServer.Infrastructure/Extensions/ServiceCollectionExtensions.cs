using Microsoft.Extensions.DependencyInjection;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Services;
using OffworldLobbyServer.Infrastructure.Services;

namespace OffworldLobbyServer.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers all infrastructure services and background cleanup services.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <returns>Service collection for chaining.</returns>
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
	{
		// Core service implementations
		services.AddSingleton<ISessionManager, InMemorySessionManager>();
		services.AddSingleton<IRoomManager, InMemoryRoomManager>();

		// SFS2X protocol processor
		services.AddTransient<SFS2XMessageProcessor>();

		// Background cleanup services
		services.AddHostedService<SessionCleanupService>();
		services.AddHostedService<RoomCleanupService>();

		return services;
	}

	/// <summary>
	/// Registers only the core infrastructure services without background services
	/// Useful for testing scenarios where you don't want background cleanup.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <returns>Service collection for chaining.</returns>
	public static IServiceCollection AddInfrastructureCoreServices(this IServiceCollection services)
	{
		// Core service implementations only
		services.AddSingleton<ISessionManager, InMemorySessionManager>();
		services.AddSingleton<IRoomManager, InMemoryRoomManager>();

		// SFS2X protocol processor
		services.AddTransient<SFS2XMessageProcessor>();

		return services;
	}

	/// <summary>
	/// Registers only the background cleanup services.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <returns>Service collection for chaining.</returns>
	public static IServiceCollection AddBackgroundCleanupServices(this IServiceCollection services)
	{
		services.AddHostedService<SessionCleanupService>();
		services.AddHostedService<RoomCleanupService>();

		return services;
	}
}
