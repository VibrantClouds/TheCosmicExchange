namespace OffworldLobbyServer.Core.Constants;

/// <summary>
/// Extension Controller Action IDs as confirmed in protocol specification.
/// These actions handle custom game logic and extension requests.
/// Source: docs/ReverseEngineeringNotes.md:160-194
/// </summary>
public static class SFS2XExtensionActions
{
	/// <summary>
	/// Extension request from client - sends custom command to server extension.
	/// Used for game-specific commands like "registerData", "gameMove", etc.
	/// </summary>
	public const byte EXTENSION_REQUEST = 0x00;

	/// <summary>
	/// Extension response from server - replies to extension request.
	/// Contains command results and any response data.
	/// </summary>
	public const byte EXTENSION_RESPONSE = 0x00; // Same as request in response context
}