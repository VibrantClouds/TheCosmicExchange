using OffworldLobbyServer.Shared.Models;

namespace OffworldLobbyServer.Core.Models;

/// <summary>
/// Represents a game room/lobby in the SFS2X system
/// Tracks players, settings, and room state.
/// </summary>
public class RoomInfo
{
	/// <summary>
	/// Unique room identifier.
	/// </summary>
	public int RoomId { get; set; }

	/// <summary>
	/// Room name (DateTime_Count format).
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Room group (always "lobbies" for game lobbies).
	/// </summary>
	public string GroupId { get; set; } = "lobbies";

	/// <summary>
	/// Maximum number of users allowed.
	/// </summary>
	public int MaxUsers { get; set; } = 10;

	/// <summary>
	/// Current number of connected users.
	/// </summary>
	public int UserCount => ConnectedUsers.Count;

	/// <summary>
	/// Room password (empty if no password).
	/// </summary>
	public string Password { get; set; } = string.Empty;

	/// <summary>
	/// Whether the room is password protected.
	/// </summary>
	public bool IsPasswordProtected => !string.IsNullOrEmpty(Password);

	/// <summary>
	/// Room owner (player who created the room).
	/// </summary>
	public CombinedID? Owner { get; set; }

	/// <summary>
	/// Unity Network GUID for P2P connection.
	/// </summary>
	public string ServerGUID { get; set; } = string.Empty;

	/// <summary>
	/// Complete lobby settings (21-element SFSArray).
	/// </summary>
	public LobbySettings Settings { get; set; } = new ();

	/// <summary>
	/// List of connected users in the room.
	/// </summary>
	public List<CombinedID> ConnectedUsers { get; set; } = new ();

	/// <summary>
	/// Map of user ready states.
	/// </summary>
	public Dictionary<CombinedID, bool> UserReadyStates { get; set; } = new ();

	/// <summary>
	/// When the room was created.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last activity in the room.
	/// </summary>
	public DateTime LastActivity { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Whether the game has started.
	/// </summary>
	public bool GameStarted { get; set; }

	/// <summary>
	/// Current room state.
	/// </summary>
	public RoomState State { get; set; } = RoomState.WaitingForPlayers;

	/// <summary>
	/// Creates a new room info.
	/// </summary>
	public RoomInfo()
	{
	}

	/// <summary>
	/// Creates a new room with specified settings.
	/// </summary>
	/// <param name="roomId">Unique room ID.</param>
	/// <param name="name">Room name.</param>
	/// <param name="maxUsers">Maximum users.</param>
	/// <param name="owner">Room owner.</param>
	public RoomInfo(int roomId, string name, int maxUsers, CombinedID? owner = null)
	{
		RoomId = roomId;
		Name = name;
		MaxUsers = maxUsers;
		Owner = owner;

		Settings = new LobbySettings(maxUsers);
		Settings.Name = name;
		Settings.LobbyID = roomId;

		UpdateActivity();
	}

	/// <summary>
	/// Adds a user to the room.
	/// </summary>
	/// <param name="user">User to add.</param>
	/// <returns>True if successfully added.</returns>
	public bool AddUser(CombinedID user)
	{
		if (ConnectedUsers.Count >= MaxUsers || ConnectedUsers.Contains(user))
		{
			return false;
		}

		ConnectedUsers.Add(user);
		UserReadyStates[user] = false;
		Settings.NumPlayersInLobby = UserCount;

		UpdateActivity();
		return true;
	}

	/// <summary>
	/// Removes a user from the room.
	/// </summary>
	/// <param name="user">User to remove.</param>
	/// <returns>True if successfully removed.</returns>
	public bool RemoveUser(CombinedID user)
	{
		var removed = ConnectedUsers.Remove(user);
		if (removed)
		{
			UserReadyStates.Remove(user);
			Settings.TeamNumbers.Remove(user.PlayerID);
			Settings.Handicaps.Remove(user.PlayerID);
			Settings.NumPlayersInLobby = UserCount;

			UpdateActivity();
		}

		return removed;
	}

	/// <summary>
	/// Sets a user's ready state.
	/// </summary>
	/// <param name="user">User to update.</param>
	/// <param name="isReady">Ready state.</param>
	public void SetUserReady(CombinedID user, bool isReady)
	{
		if (ConnectedUsers.Contains(user))
		{
			UserReadyStates[user] = isReady;
			UpdateActivity();
		}
	}

	/// <summary>
	/// Checks if a user is ready.
	/// </summary>
	/// <param name="user">User to check.</param>
	/// <returns>True if ready.</returns>
	public bool IsUserReady(CombinedID user)
	{
		return UserReadyStates.TryGetValue(user, out var ready) && ready;
	}

	/// <summary>
	/// Checks if all users are ready.
	/// </summary>
	/// <returns>True if all users are ready.</returns>
	public bool AreAllUsersReady()
	{
		return ConnectedUsers.Count >= 2 &&
			   ConnectedUsers.All(user => IsUserReady(user));
	}

	/// <summary>
	/// Gets the number of ready users.
	/// </summary>
	/// <returns>Ready user count.</returns>
	public int GetReadyUserCount()
	{
		return ConnectedUsers.Count(IsUserReady);
	}

	/// <summary>
	/// Checks if the room can start a game.
	/// </summary>
	/// <returns>True if ready to start.</returns>
	public bool CanStartGame()
	{
		return State == RoomState.WaitingForPlayers &&
			   ConnectedUsers.Count >= 2 &&
			   AreAllUsersReady() &&
			   !string.IsNullOrEmpty(Settings.Name);
	}

	/// <summary>
	/// Starts the game.
	/// </summary>
	public void StartGame()
	{
		if (CanStartGame())
		{
			GameStarted = true;
			State = RoomState.GameInProgress;
			UpdateActivity();
		}
	}

	/// <summary>
	/// Validates the room password.
	/// </summary>
	/// <param name="password">Password to check.</param>
	/// <returns>True if password is correct or room has no password.</returns>
	public bool ValidatePassword(string password)
	{
		if (!IsPasswordProtected)
		{
			return true;
		}

		return Password == password;
	}

	/// <summary>
	/// Updates the last activity timestamp.
	/// </summary>
	public void UpdateActivity()
	{
		LastActivity = DateTime.UtcNow;
	}

	/// <summary>
	/// Checks if the room is empty and should be cleaned up.
	/// </summary>
	/// <param name="timeoutMinutes">Timeout in minutes.</param>
	/// <returns>True if should be cleaned up.</returns>
	public bool ShouldCleanup(int timeoutMinutes = 60)
	{
		return ConnectedUsers.Count == 0 &&
			   DateTime.UtcNow.Subtract(LastActivity).TotalMinutes > timeoutMinutes;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		var passwordInfo = IsPasswordProtected ? " (Password)" : string.Empty;
		var readyInfo = $"({GetReadyUserCount()}/{UserCount} ready)";
		return $"Room {RoomId}: '{Name}' {readyInfo}{passwordInfo} - {State}";
	}
}

/// <summary>
/// Possible room states.
/// </summary>
public enum RoomState
{
	WaitingForPlayers,
	GameInProgress,
	GameCompleted,
	Abandoned,
}
