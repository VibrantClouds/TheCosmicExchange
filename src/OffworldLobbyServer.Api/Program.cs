using OffworldLobbyServer.Api.Controllers;
using OffworldLobbyServer.Api.Middleware;
using OffworldLobbyServer.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add infrastructure services (session manager, room manager, cleanup services)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add logging configuration
builder.Services.AddLogging(logging =>
{
	logging.AddConsole();
	logging.AddDebug();
});

// Add OpenAPI/Swagger for development
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/openapi/v1.json", "Offworld Lobby Server API V1");
	});
}

// Add protocol detection middleware
app.UseMiddleware<ProtocolDetectionMiddleware>();

// Add BlueBox middleware for proper header handling
app.UseMiddleware<BlueBoxMiddleware>();

// Use controllers
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => new
{
	Status = "Healthy",
	Timestamp = DateTime.UtcNow,
	Version = "1.0.0",
	Environment = app.Environment.EnvironmentName,
})
.WithName("HealthCheck")
.WithOpenApi();

// Add diagnostics endpoint for monitoring
app.MapGet("/diagnostics", async (IServiceProvider serviceProvider) =>
{
	var sessionManager = serviceProvider.GetRequiredService<OffworldLobbyServer.Core.Interfaces.ISessionManager>();
	var roomManager = serviceProvider.GetRequiredService<OffworldLobbyServer.Core.Interfaces.IRoomManager>();
	var tcpService = serviceProvider.GetRequiredService<OffworldLobbyServer.Core.Interfaces.ISFS2XTcpService>();

	var sessionDiagnostics = await sessionManager.GetDiagnosticsAsync();
	var roomDiagnostics = await roomManager.GetDiagnosticsAsync();
	var tcpServiceStatus = await tcpService.GetStatusAsync();

	return new
	{
		Sessions = sessionDiagnostics,
		Rooms = roomDiagnostics,
		Protocols = new
		{
			BlueBoxHttp = new
			{
				Port = 8080,
				Enabled = true,
				Transport = "HTTP POST /BlueBox/BlueBox.do",
				Description = "HTTP-tunneled SFS2X protocol (fallback)"
			},
			SFS2XDirect = new
			{
				Port = tcpServiceStatus.Port,
				Enabled = tcpServiceStatus.IsRunning,
				Transport = "TCP Binary",
				Description = "Direct SFS2X binary protocol (primary)",
				ActiveConnections = tcpServiceStatus.ActiveConnections,
				TotalConnections = tcpServiceStatus.TotalConnections,
				StartTime = tcpServiceStatus.StartTime
			}
		},
		Server = new
		{
			Environment = app.Environment.EnvironmentName,
			MachineName = Environment.MachineName,
			ProcessId = Environment.ProcessId,
			WorkingSet = Environment.WorkingSet,
			Timestamp = DateTime.UtcNow
		},
	};
})
.WithName("GetDiagnostics")
.WithOpenApi();

app.Run();
