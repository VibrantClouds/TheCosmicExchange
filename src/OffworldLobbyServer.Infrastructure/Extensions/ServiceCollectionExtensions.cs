using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
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
	/// <param name="configuration">Configuration for server settings.</param>
	/// <returns>Service collection for chaining.</returns>
	public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration? configuration = null)
	{
		// Configure server settings
		if (configuration != null)
		{
			var section = configuration.GetSection("OffworldServer");
			services.Configure<ServerConfiguration>(options =>
			{
				// Manually bind the configuration values using basic string parsing
				var portsSection = section.GetSection("Ports");
				if (portsSection.Exists())
				{
					var sfs2xPort = portsSection.GetSection("SFS2XDirect").Value;
					if (!string.IsNullOrEmpty(sfs2xPort) && int.TryParse(sfs2xPort, out var port1))
					{
						options.Ports.SFS2XDirect = port1;
					}
					
					var blueBoxPort = portsSection.GetSection("BlueBoxHttp").Value;
					if (!string.IsNullOrEmpty(blueBoxPort) && int.TryParse(blueBoxPort, out var port2))
					{
						options.Ports.BlueBoxHttp = port2;
					}
				}
				
				var protocolSection = section.GetSection("Protocol");
				if (protocolSection.Exists())
				{
					var enableSFS2X = protocolSection.GetSection("EnableSFS2XDirect").Value;
					if (!string.IsNullOrEmpty(enableSFS2X) && bool.TryParse(enableSFS2X, out var enable1))
					{
						options.Protocol.EnableSFS2XDirect = enable1;
					}
					
					var enableBlueBox = protocolSection.GetSection("EnableBlueBoxHttp").Value;
					if (!string.IsNullOrEmpty(enableBlueBox) && bool.TryParse(enableBlueBox, out var enable2))
					{
						options.Protocol.EnableBlueBoxHttp = enable2;
					}
				}
			});
		}
		else
		{
			// Use default configuration if none provided
			services.Configure<ServerConfiguration>(_ => { });
		}

		// Core service implementations
		services.AddSingleton<ISessionManager, InMemorySessionManager>();
		services.AddSingleton<IRoomManager, InMemoryRoomManager>();

		// SFS2X protocol services
		services.AddTransient<SFS2XMessageProcessor>();
		services.AddTransient<SFS2XBinaryMessageProcessor>();
		// TCP service disabled for BlueBox-only experiment
		// services.AddSingleton<ISFS2XTcpService, SFS2XTcpService>();

		// Hosted services for dual-protocol support  
		// TCP hosted service disabled for BlueBox-only experiment
		// services.AddHostedService<SFS2XTcpHostedService>();
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
