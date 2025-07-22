using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OffworldLobbyServer.Core.Interfaces;
using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Models;

namespace OffworldLobbyServer.Infrastructure.Services;

/// <summary>
/// In-memory implementation of room/lobby management
/// Uses thread-safe ConcurrentDictionary for room storage
/// Provides complete SFS2X room lifecycle management.
/// </summary>
public class InMemoryRoomManager : IRoomManager
{
	private readonly ConcurrentDictionary<int, RoomInfo> _rooms = new ();
	private readonly ILogger<InMemoryRoomManager> _logger;
	private volatile int _nextRoomId = 1;

	public InMemoryRoomManager(ILogger<InMemoryRoomManager> logger)
	{
		_logger = logger;
		_logger.LogInformation("InMemoryRoomManager initialized");
	}

	/// <summary>
	/// Creates a new room with the specified settings.
	/// </summary>
	/// <param name="settings">Lobby configuration.</param>
	/// <param name="owner">Room owner.</param>
	/// <param name="password">Optional room password.</param>
	/// <returns>Created room information.</returns>
	public Task<RoomInfo> CreateRoomAsync(LobbySettings settings, CombinedID owner, string password = "")
	{
		var roomId = Interlocked.Increment(ref _nextRoomId);
		var roomName = GenerateRoomName(roomId);

		var room = new RoomInfo(roomId, roomName, settings.MaxPlayers, owner)
		{
			Password = password ?? string.Empty,
			ServerGUID = GenerateUnityGUID(),
			Settings = settings.Copy(), // Create a copy to avoid reference issues
			GroupId = "lobbies",
		};

		// Update settings with room-specific values
		room.Settings.LobbyID = roomId;
		room.Settings.Name = roomName;
		room.Settings.ServerGUID = room.ServerGUID;
		room.Settings.OwnerID = owner.PlayerID;

		// Add the owner to the room
		room.AddUser(owner);

		_rooms[roomId] = room;

		_logger.LogInformation(
			"Created room {RoomId} '{RoomName}' for owner {Owner} (max users: {MaxUsers})",
			roomId, roomName, owner.GetPlayerID(), settings.MaxPlayers);

		return Task.FromResult(room);
	}

	/// <summary>
	/// Gets a room by its ID.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <returns>Room info or null if not found.</returns>
	public Task<RoomInfo?> GetRoomAsync(int roomId)
	{
		_rooms.TryGetValue(roomId, out var room);
		return Task.FromResult(room);
	}

	/// <summary>
	/// Gets all rooms in a group (e.g., "lobbies").
	/// </summary>
	/// <param name="groupId">Group identifier.</param>
	/// <returns>List of rooms in the group.</returns>
	public Task<IEnumerable<RoomInfo>> GetRoomsInGroupAsync(string groupId = "lobbies")
	{
		var roomsInGroup = _rooms.Values
			.Where(room => room.GroupId.Equals(groupId, StringComparison.OrdinalIgnoreCase))
			.ToList();

		return Task.FromResult<IEnumerable<RoomInfo>>(roomsInGroup);
	}

	/// <summary>
	/// Adds a user to a room.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="user">User to add.</param>
	/// <param name="password">Room password if required.</param>
	/// <returns>True if successfully joined.</returns>
	public Task<bool> JoinRoomAsync(int roomId, CombinedID user, string password = "")
	{
		if (!_rooms.TryGetValue(roomId, out var room))
		{
			_logger.LogWarning("Attempted to join non-existent room {RoomId}", roomId);
			return Task.FromResult(false);
		}

		lock (room)
		{
			// Check if room is in correct state
			if (room.State != RoomState.WaitingForPlayers)
			{
				_logger.LogWarning(
					"User {User} attempted to join room {RoomId} in state {State}",
					user.GetPlayerID(), roomId, room.State);
				return Task.FromResult(false);
			}

			// Validate password
			if (!room.ValidatePassword(password))
			{
				_logger.LogWarning(
					"User {User} provided incorrect password for room {RoomId}",
					user.GetPlayerID(), roomId);
				return Task.FromResult(false);
			}

			// Try to add user
			if (!room.AddUser(user))
			{
				_logger.LogWarning(
					"Failed to add user {User} to room {RoomId} (room full or already joined)",
					user.GetPlayerID(), roomId);
				return Task.FromResult(false);
			}

			_logger.LogDebug(
				"User {User} joined room {RoomId} ({UserCount}/{MaxUsers})",
				user.GetPlayerID(), roomId, room.UserCount, room.MaxUsers);

			return Task.FromResult(true);
		}
	}

	/// <summary>
	/// Removes a user from a room.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="user">User to remove.</param>
	/// <returns>True if successfully left.</returns>
	public Task<bool> LeaveRoomAsync(int roomId, CombinedID user)
	{
		if (!_rooms.TryGetValue(roomId, out var room))
		{
			_logger.LogWarning("Attempted to leave non-existent room {RoomId}", roomId);
			return Task.FromResult(false);
		}

		lock (room)
		{
			if (!room.RemoveUser(user))
			{
				_logger.LogWarning(
					"User {User} was not in room {RoomId}",
					user.GetPlayerID(), roomId);
				return Task.FromResult(false);
			}

			_logger.LogDebug(
				"User {User} left room {RoomId} ({UserCount}/{MaxUsers})",
				user.GetPlayerID(), roomId, room.UserCount, room.MaxUsers);

			// Handle owner leaving
			if (room.Owner?.Equals(user) == true)
			{
				if (room.UserCount > 0)
				{
					// Transfer ownership to first remaining user
					room.Owner = room.ConnectedUsers.First();
					room.Settings.OwnerID = room.Owner.PlayerID;
					_logger.LogDebug(
						"Transferred ownership of room {RoomId} to {NewOwner}",
						roomId, room.Owner.GetPlayerID());
				}
				else
				{
					// Room is empty, mark for cleanup
					room.State = RoomState.Abandoned;
					_logger.LogDebug("Room {RoomId} abandoned (no users remaining)", roomId);
				}
			}

			return Task.FromResult(true);
		}
	}

	/// <summary>
	/// Sets a user's ready state in a room.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="user">User to update.</param>
	/// <param name="isReady">Ready state.</param>
	/// <returns>True if successfully updated.</returns>
	public Task<bool> SetUserReadyAsync(int roomId, CombinedID user, bool isReady)
	{
		if (!_rooms.TryGetValue(roomId, out var room))
		{
			_logger.LogWarning("Attempted to set ready state in non-existent room {RoomId}", roomId);
			return Task.FromResult(false);
		}

		lock (room)
		{
			if (!room.ConnectedUsers.Contains(user))
			{
				_logger.LogWarning(
					"User {User} not in room {RoomId}, cannot set ready state",
					user.GetPlayerID(), roomId);
				return Task.FromResult(false);
			}

			room.SetUserReady(user, isReady);

			_logger.LogDebug(
				"Set user {User} ready state to {IsReady} in room {RoomId} ({ReadyCount}/{UserCount} ready)",
				user.GetPlayerID(), isReady, roomId, room.GetReadyUserCount(), room.UserCount);

			return Task.FromResult(true);
		}
	}

	/// <summary>
	/// Updates room settings (owner only).
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="settings">New settings.</param>
	/// <param name="requestingUser">User making the request.</param>
	/// <returns>True if successfully updated.</returns>
	public Task<bool> UpdateRoomSettingsAsync(int roomId, LobbySettings settings, CombinedID requestingUser)
	{
		if (!_rooms.TryGetValue(roomId, out var room))
		{
			_logger.LogWarning("Attempted to update settings for non-existent room {RoomId}", roomId);
			return Task.FromResult(false);
		}

		lock (room)
		{
			// Check if user is the owner
			if (!room.Owner?.Equals(requestingUser) == true)
			{
				_logger.LogWarning(
					"User {User} attempted to update settings for room {RoomId} but is not owner",
					requestingUser.GetPlayerID(), roomId);
				return Task.FromResult(false);
			}

			// Check if room is in correct state
			if (room.State != RoomState.WaitingForPlayers)
			{
				_logger.LogWarning(
					"Cannot update settings for room {RoomId} in state {State}",
					roomId, room.State);
				return Task.FromResult(false);
			}

			// Preserve certain room-specific values
			var newSettings = settings.Copy();
			newSettings.LobbyID = roomId;
			newSettings.Name = room.Name;
			newSettings.ServerGUID = room.ServerGUID;
			newSettings.OwnerID = room.Owner?.PlayerID;
			newSettings.NumPlayersInLobby = room.UserCount;

			// Update room settings
			room.Settings = newSettings;

			// Update max users if it changed
			if (room.MaxUsers != settings.MaxPlayers)
			{
				room.MaxUsers = settings.MaxPlayers;

				// If room is now over capacity, remove excess users (FIFO)
				while (room.UserCount > room.MaxUsers)
				{
					var userToRemove = room.ConnectedUsers.First();
					room.RemoveUser(userToRemove);
					_logger.LogWarning(
						"Removed user {User} from room {RoomId} due to capacity reduction",
						userToRemove.GetPlayerID(), roomId);
				}
			}

			_logger.LogDebug(
				"Updated settings for room {RoomId} by owner {Owner}",
				roomId, requestingUser.GetPlayerID());

			return Task.FromResult(true);
		}
	}

	/// <summary>
	/// Starts a game in the room if all conditions are met.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <param name="requestingUser">User requesting game start.</param>
	/// <returns>True if game started successfully.</returns>
	public Task<bool> StartGameAsync(int roomId, CombinedID requestingUser)
	{
		if (!_rooms.TryGetValue(roomId, out var room))
		{
			_logger.LogWarning("Attempted to start game in non-existent room {RoomId}", roomId);
			return Task.FromResult(false);
		}

		lock (room)
		{
			// Check if user is the owner
			if (!room.Owner?.Equals(requestingUser) == true)
			{
				_logger.LogWarning(
					"User {User} attempted to start game in room {RoomId} but is not owner",
					requestingUser.GetPlayerID(), roomId);
				return Task.FromResult(false);
			}

			// Check if game can start
			if (!room.CanStartGame())
			{
				_logger.LogWarning(
					"Cannot start game in room {RoomId}: State={State}, Users={UserCount}, Ready={ReadyCount}",
					roomId, room.State, room.UserCount, room.GetReadyUserCount());
				return Task.FromResult(false);
			}

			// Start the game
			room.StartGame();

			_logger.LogInformation(
				"Game started in room {RoomId} by {Owner} with {UserCount} players",
				roomId, requestingUser.GetPlayerID(), room.UserCount);

			return Task.FromResult(true);
		}
	}

	/// <summary>
	/// Removes a room from the system.
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <returns>True if room was found and removed.</returns>
	public Task<bool> RemoveRoomAsync(int roomId)
	{
		if (_rooms.TryRemove(roomId, out var room))
		{
			lock (room)
			{
				room.State = RoomState.Abandoned;
				_logger.LogDebug("Removed room {RoomId} '{RoomName}'", roomId, room.Name);
			}

			return Task.FromResult(true);
		}

		_logger.LogWarning("Attempted to remove non-existent room {RoomId}", roomId);
		return Task.FromResult(false);
	}

	/// <summary>
	/// Cleans up abandoned or expired rooms.
	/// </summary>
	/// <param name="timeoutMinutes">Room timeout in minutes.</param>
	/// <returns>Number of rooms cleaned up.</returns>
	public Task<int> CleanupAbandonedRoomsAsync(int timeoutMinutes = 60)
	{
		var roomsToRemove = new List<int>();

		// First pass: identify rooms to clean up
		foreach (var kvp in _rooms)
		{
			var room = kvp.Value;
			lock (room)
			{
				if (room.ShouldCleanup(timeoutMinutes) || room.State == RoomState.Abandoned)
				{
					roomsToRemove.Add(kvp.Key);
				}
			}
		}

		// Second pass: remove identified rooms
		int cleanedUp = 0;
		foreach (var roomId in roomsToRemove)
		{
			if (_rooms.TryRemove(roomId, out var room))
			{
				lock (room)
				{
					room.State = RoomState.Abandoned;
					cleanedUp++;
				}
			}
		}

		if (cleanedUp > 0)
		{
			_logger.LogInformation(
				"Cleaned up {Count} abandoned/expired rooms (timeout: {TimeoutMinutes} minutes)",
				cleanedUp, timeoutMinutes);
		}

		return Task.FromResult(cleanedUp);
	}

	/// <summary>
	/// Gets the number of rooms in a group.
	/// </summary>
	/// <param name="groupId">Group identifier.</param>
	/// <returns>Room count.</returns>
	public Task<int> GetRoomCountAsync(string groupId = "lobbies")
	{
		var count = _rooms.Values.Count(room =>
			room.GroupId.Equals(groupId, StringComparison.OrdinalIgnoreCase));

		return Task.FromResult(count);
	}

	/// <summary>
	/// Finds rooms that a user can join (not full, correct password, etc.)
	/// </summary>
	/// <param name="groupId">Group to search.</param>
	/// <param name="maxResults">Maximum number of results.</param>
	/// <returns>List of joinable rooms.</returns>
	public Task<IEnumerable<RoomInfo>> FindJoinableRoomsAsync(string groupId = "lobbies", int maxResults = 50)
	{
		var joinableRooms = _rooms.Values
			.Where(room => room.GroupId.Equals(groupId, StringComparison.OrdinalIgnoreCase))
			.Where(room => room.State == RoomState.WaitingForPlayers)
			.Where(room => room.UserCount < room.MaxUsers)
			.OrderBy(room => room.CreatedAt)
			.Take(maxResults)
			.ToList();

		return Task.FromResult<IEnumerable<RoomInfo>>(joinableRooms);
	}

	/// <summary>
	/// Gets room by owner.
	/// </summary>
	/// <param name="owner">Room owner.</param>
	/// <returns>Room owned by the user or null.</returns>
	public Task<RoomInfo?> GetRoomByOwnerAsync(CombinedID owner)
	{
		var ownedRoom = _rooms.Values
			.FirstOrDefault(room => room.Owner?.Equals(owner) == true);

		return Task.FromResult(ownedRoom);
	}

	/// <summary>
	/// Generates a Unity-compatible GUID for P2P networking
	/// Format: 32 hex characters without dashes.
	/// </summary>
	/// <returns>Unity GUID string.</returns>
	private static string GenerateUnityGUID()
	{
		return Guid.NewGuid().ToString("N");
	}

	/// <summary>
	/// Generates a room name with timestamp and counter
	/// Format: DateTime_RoomId (e.g., "2024-01-15_14:30:25_001").
	/// </summary>
	/// <param name="roomId">Room identifier.</param>
	/// <returns>Generated room name.</returns>
	private static string GenerateRoomName(int roomId)
	{
		var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss");
		return $"{timestamp}_{roomId:D3}";
	}

	/// <summary>
	/// Gets diagnostics information for monitoring.
	/// </summary>
	/// <returns>Room manager statistics.</returns>
	public Task<object> GetDiagnosticsAsync()
	{
		var totalRooms = _rooms.Count;
		var waitingRooms = 0;
		var gameInProgressRooms = 0;
		var abandonedRooms = 0;
		var passwordProtectedRooms = 0;
		var totalUsers = 0;
		var totalReadyUsers = 0;
		var fullRooms = 0;

		foreach (var room in _rooms.Values)
		{
			lock (room)
			{
				switch (room.State)
				{
					case RoomState.WaitingForPlayers:
						waitingRooms++;
						break;
					case RoomState.GameInProgress:
						gameInProgressRooms++;
						break;
					case RoomState.Abandoned:
						abandonedRooms++;
						break;
				}

				if (room.IsPasswordProtected)
				{
					passwordProtectedRooms++;
				}

				if (room.UserCount >= room.MaxUsers)
				{
					fullRooms++;
				}

				totalUsers += room.UserCount;
				totalReadyUsers += room.GetReadyUserCount();
			}
		}

		var diagnostics = new
		{
			TotalRooms = totalRooms,
			WaitingForPlayersRooms = waitingRooms,
			GameInProgressRooms = gameInProgressRooms,
			AbandonedRooms = abandonedRooms,
			PasswordProtectedRooms = passwordProtectedRooms,
			FullRooms = fullRooms,
			TotalUsers = totalUsers,
			TotalReadyUsers = totalReadyUsers,
			AverageUsersPerRoom = totalRooms > 0 ? (double)totalUsers / totalRooms : 0.0,
			ReadyUserPercentage = totalUsers > 0 ? (double)totalReadyUsers / totalUsers * 100.0 : 0.0,
			NextRoomId = _nextRoomId,
			Timestamp = DateTime.UtcNow,
		};

		return Task.FromResult<object>(diagnostics);
	}
}
