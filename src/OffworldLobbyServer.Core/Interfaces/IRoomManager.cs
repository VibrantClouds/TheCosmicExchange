using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Models;

namespace OffworldLobbyServer.Core.Interfaces;

/// <summary>
/// Manages SFS2X room/lobby creation, joining, and lifecycle.
/// </summary>
public interface IRoomManager
{
	/// <summary>
	/// Creates a new room with the specified settings.
	/// </summary>
	/// <param name="settings">Lobby configuration.</param>
	/// <param name="owner">Room owner.</param>
	/// <param name="password">Optional room password.</param>
	/// <returns>Created room information.</returns>
	Task<RoomInfo> CreateRoomAsync(LobbySettings settings, CombinedID owner, string password = "");

	/// <summary>
	/// Gets a room by its ID.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <returns>Room info or null if not found.</returns>
	Task<RoomInfo?> GetRoomAsync(int roomId);

	/// <summary>
	/// Gets all rooms in a group (e.g., "lobbies").
	/// </summary>
	/// <param name="groupId">Group identifier.</param>
	/// <returns>List of rooms in the group.</returns>
	Task<IEnumerable<RoomInfo>> GetRoomsInGroupAsync(string groupId = "lobbies");

	/// <summary>
	/// Adds a user to a room.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="user">User to add.</param>
	/// <param name="password">Room password if required.</param>
	/// <returns>True if successfully joined.</returns>
	Task<bool> JoinRoomAsync(int roomId, CombinedID user, string password = "");

	/// <summary>
	/// Removes a user from a room.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="user">User to remove.</param>
	/// <returns>True if successfully left.</returns>
	Task<bool> LeaveRoomAsync(int roomId, CombinedID user);

	/// <summary>
	/// Sets a user's ready state in a room.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="user">User to update.</param>
	/// <param name="isReady">Ready state.</param>
	/// <returns>True if successfully updated.</returns>
	Task<bool> SetUserReadyAsync(int roomId, CombinedID user, bool isReady);

	/// <summary>
	/// Updates room settings (owner only).
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="settings">New settings.</param>
	/// <param name="requestingUser">User making the request.</param>
	/// <returns>True if successfully updated.</returns>
	Task<bool> UpdateRoomSettingsAsync(int roomId, LobbySettings settings, CombinedID requestingUser);

	/// <summary>
	/// Starts a game in the room if all conditions are met.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="requestingUser">User requesting game start.</param>
	/// <returns>True if game started successfully.</returns>
	Task<bool> StartGameAsync(int roomId, CombinedID requestingUser);

	/// <summary>
	/// Removes a room from the system.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <returns>True if room was found and removed.</returns>
	Task<bool> RemoveRoomAsync(int roomId);

	/// <summary>
	/// Cleans up abandoned or expired rooms.
	/// </summary>
	/// <param name="timeoutMinutes">Room timeout in minutes.</param>
	/// <returns>Number of rooms cleaned up.</returns>
	Task<int> CleanupAbandonedRoomsAsync(int timeoutMinutes = 60);

	/// <summary>
	/// Gets the number of rooms in a group.
	/// </summary>
	/// <param name="groupId">Group identifier.</param>
	/// <returns>Room count.</returns>
	Task<int> GetRoomCountAsync(string groupId = "lobbies");

	/// <summary>
	/// Finds rooms that a user can join (not full, correct password, etc.)
	/// </summary>
	/// <param name="groupId">Group to search.</param>
	/// <param name="maxResults">Maximum number of results.</param>
	/// <returns>List of joinable rooms.</returns>
	Task<IEnumerable<RoomInfo>> FindJoinableRoomsAsync(string groupId = "lobbies", int maxResults = 50);

	/// <summary>
	/// Gets room by owner.
	/// </summary>
	/// <param name="owner">Room owner.</param>
	/// <returns>Room owned by the user or null.</returns>
	Task<RoomInfo?> GetRoomByOwnerAsync(CombinedID owner);

	/// <summary>
	/// Gets diagnostics information for monitoring.
	/// </summary>
	/// <returns>Room manager statistics.</returns>
	Task<object> GetDiagnosticsAsync();
}
