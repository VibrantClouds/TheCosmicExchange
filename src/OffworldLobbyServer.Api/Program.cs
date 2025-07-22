using OffworldLobbyServer.Api.Controllers;
using OffworldLobbyServer.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add infrastructure services (session manager, room manager, cleanup services)
builder.Services.AddInfrastructureServices();

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

	var sessionDiagnostics = await sessionManager.GetDiagnosticsAsync();
	var roomDiagnostics = await roomManager.GetDiagnosticsAsync();

	return new
	{
		Sessions = sessionDiagnostics,
		Rooms = roomDiagnostics,
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
