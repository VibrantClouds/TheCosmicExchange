namespace OffworldLobbyServer.Core.Models;

/// <summary>
/// Configuration model for the Offworld server settings.
/// </summary>
public class ServerConfiguration
{
	/// <summary>
	/// Port configuration settings.
	/// </summary>
	public PortSettings Ports { get; set; } = new();

	/// <summary>
	/// Protocol configuration settings.
	/// </summary>
	public ProtocolSettings Protocol { get; set; } = new();
}

/// <summary>
/// Port configuration for different service endpoints.
/// </summary>
public class PortSettings
{
	/// <summary>
	/// Port for BlueBox HTTP transport (default: 8080).
	/// </summary>
	public int BlueBoxHttp { get; set; } = 8080;

	/// <summary>
	/// Port for direct SFS2X connections (default: 9933).
	/// </summary>
	public int SFS2XDirect { get; set; } = 9933;

	/// <summary>
	/// Description of port usage.
	/// </summary>
	public string Description { get; set; } = "BlueBoxHttp: HTTP transport for SFS2X protocol, SFS2XDirect: Primary SFS2X connection port";
}

/// <summary>
/// Protocol support configuration.
/// </summary>
public class ProtocolSettings
{
	/// <summary>
	/// Enable BlueBox HTTP transport on port 8080 (default: true).
	/// </summary>
	public bool EnableBlueBoxHttp { get; set; } = true;

	/// <summary>
	/// Enable direct SFS2X binary protocol on port 9933 (default: true).
	/// </summary>
	public bool EnableSFS2XDirect { get; set; } = true;

	/// <summary>
	/// Description of protocol settings.
	/// </summary>
	public string Description { get; set; } = "Protocol support configuration";
}