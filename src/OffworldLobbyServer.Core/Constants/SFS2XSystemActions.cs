namespace OffworldLobbyServer.Core.Constants;

/// <summary>
/// System Controller Action IDs as confirmed in protocol specification.
/// These actions handle core SFS2X server functionality.
/// Source: docs/ReverseEngineeringNotes.md:52-58
/// </summary>
public static class SFS2XSystemActions
{
	/// <summary>
	/// Client handshake request - establishes protocol version and capabilities.
	/// </summary>
	public const byte HANDSHAKE = 0x00;

	/// <summary>
	/// User login request - authenticates user to zone.
	/// </summary>
	public const byte LOGIN_REQUEST = 0x01;

	/// <summary>
	/// Login error response - indicates authentication failure.
	/// </summary>
	public const byte LOGIN_ERROR = 0x02;

	/// <summary>
	/// Join room request - user requests to join a specific room.
	/// </summary>
	public const byte JOIN_ROOM = 0x04;

	/// <summary>
	/// User enter room event - notifies when user joins room.
	/// </summary>
	public const byte USER_ENTER_ROOM = 0x05;

	/// <summary>
	/// Proximity list update - updates nearby users/objects.
	/// </summary>
	public const byte PROXIMITY_LIST_UPDATE = 0x07;
}