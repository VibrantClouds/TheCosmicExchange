namespace OffworldLobbyServer.Core.Constants;

/// <summary>
/// SFS2X Controller IDs as confirmed in protocol specification.
/// Controllers represent different subsystems within SFS2X protocol.
/// </summary>
public static class SFS2XControllers
{
	/// <summary>
	/// System Controller - handles core server functions.
	/// Used for: handshake, login, room operations, user management.
	/// </summary>
	public const byte SYSTEM = 0x00;

	/// <summary>
	/// Extension Controller - handles custom game logic.
	/// Used for: extension requests, custom commands, game-specific messaging.
	/// </summary>
	public const byte EXTENSION = 0x01;
}