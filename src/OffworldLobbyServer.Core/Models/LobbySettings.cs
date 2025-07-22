using System.Collections.ObjectModel;
using OffworldLobbyServer.Shared.Enums;
using OffworldLobbyServer.Shared.Models;

namespace OffworldLobbyServer.Core.Models;

/// <summary>
/// Complete lobby configuration matching the game's MLobbySettings
/// Represents the 21-element SFSArray structure exactly.
/// </summary>
public class LobbySettings
{
	/// <summary>
	/// Creates a new LobbySettings with default multiplayer configuration.
	/// </summary>
	public LobbySettings()
	{
	}

	/// <summary>
	/// Creates a new LobbySettings with specified max players and lobby type.
	/// </summary>
	/// <param name="maxPlayers">Maximum players allowed.</param>
	/// <param name="lobbyType">Type of lobby to create.</param>
	public LobbySettings(int maxPlayers, LobbyType lobbyType = LobbyType.Multiplayer)
	{
		MaxPlayers = maxPlayers;
		KindOfLobby = lobbyType;

		// Initialize default arrays
		InvalidHumanHQ = new bool[maxPlayers];
		GameOptions = new bool[32]; // Assume 32 game options
	}

	/// <summary>
	/// Unique lobby identifier.
	/// </summary>
	public int LobbyID { get; set; }

	/// <summary>
	/// Maximum number of players allowed in lobby.
	/// </summary>
	public int MaxPlayers { get; set; } = 10;

	/// <summary>
	/// Current number of players in lobby.
	/// </summary>
	public int NumPlayersInLobby { get; set; }

	/// <summary>
	/// Player who owns/created the lobby.
	/// </summary>
	public PlayerID? OwnerID { get; set; }

	/// <summary>
	/// Unity Network GUID for P2P connection.
	/// </summary>
	public string ServerGUID { get; set; } = string.Empty;

	/// <summary>
	/// Lobby password for private games.
	/// </summary>
	public string Password { get; set; } = string.Empty;

	// 21-Element SFSArray Structure (matching indices from NETWORKING.md)

	/// <summary>
	/// Index 0: Lobby display name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Index 1: Type of lobby (Multiplayer, Tournament, etc.)
	/// </summary>
	public LobbyType KindOfLobby { get; set; } = LobbyType.Multiplayer;

	/// <summary>
	/// Index 2: Steam version compatibility key.
	/// </summary>
	public string SteamVersionKey { get; set; } = string.Empty;

	/// <summary>
	/// Index 3: Game setup configuration.
	/// </summary>
	public GameSetupType GameSetup { get; set; } = GameSetupType.Standard;

	/// <summary>
	/// Index 4: Rules set being used.
	/// </summary>
	public RulesSetType RulesSet { get; set; } = RulesSetType.Standard;

	/// <summary>
	/// Index 5: Whether to record replay file.
	/// </summary>
	public bool WantReplayFile { get; set; }

	/// <summary>
	/// Index 6: Map location/theme.
	/// </summary>
	public LocationType Location { get; set; } = LocationType.Mars;

	/// <summary>
	/// Index 7: Invalid human HQ restrictions (array of booleans).
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Protocol compatibility requires array type for SFS2X serialization")]
	public bool[] InvalidHumanHQ { get; internal set; } = Array.Empty<bool>();

	/// <summary>
	/// Index 8: Whether AI players should fill empty slots.
	/// </summary>
	public bool WantAIplayers { get; set; } = true;

	/// <summary>
	/// Index 9: Map size setting.
	/// </summary>
	public MapSizeType MapSizeIndex { get; set; } = MapSizeType.Medium;

	/// <summary>
	/// Index 10: Terrain generation type.
	/// </summary>
	public TerrainClassType TerrainClassIndex { get; set; } = TerrainClassType.Open;

	/// <summary>
	/// Index 11: Game speed setting.
	/// </summary>
	public GameSpeedType GameSpeedIndex { get; set; } = GameSpeedType.Normal;

	/// <summary>
	/// Index 12: Custom map name (empty for procedural maps).
	/// </summary>
	public string MapName { get; set; } = string.Empty;

	/// <summary>
	/// Index 13: Random seed for procedural generation.
	/// </summary>
	public int Seed { get; set; }

	/// <summary>
	/// Index 14: Map latitude setting.
	/// </summary>
	public LatitudeType Latitude { get; set; } = LatitudeType.Equatorial;

	/// <summary>
	/// Index 15: Resource minimum setting.
	/// </summary>
	public ResourceMinimumType ResourceMinimum { get; set; } = ResourceMinimumType.Normal;

	/// <summary>
	/// Index 16: Resource presence setting.
	/// </summary>
	public ResourcePresenceType ResourcePresence { get; set; } = ResourcePresenceType.Normal;

	/// <summary>
	/// Index 17: Colony class type.
	/// </summary>
	public ColonyClassType ColonyClass { get; set; } = ColonyClassType.Mining;

	/// <summary>
	/// Index 18: Game options (array of various boolean settings).
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Protocol compatibility requires array type for SFS2X serialization")]
	public bool[] GameOptions { get; internal set; } = Array.Empty<bool>();

	/// <summary>
	/// Index 19: Team assignments - maps PlayerID to team number
	/// SFSObject structure: PlayerID -> team number (short).
	/// Protocol compatibility requires mutable dictionary access.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Protocol compatibility requires mutable access for SFS2X serialization")]
	public Dictionary<PlayerID, short> TeamNumbers { get; set; } = new ();

	/// <summary>
	/// Index 20: Handicap assignments - maps PlayerID to handicap value
	/// SFSObject structure: PlayerID -> handicap value (short).
	/// Protocol compatibility requires mutable dictionary access.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Protocol compatibility requires mutable access for SFS2X serialization")]
	public Dictionary<PlayerID, short> Handicaps { get; set; } = new ();

	/// <summary>
	/// Checks if the lobby is ready to start a game.
	/// </summary>
	/// <returns>True if ready to start.</returns>
	public bool IsReadyToStart()
	{
		return NumPlayersInLobby >= 2 && !string.IsNullOrEmpty(Name);
	}

	/// <summary>
	/// Gets the number of human players (non-AI).
	/// </summary>
	/// <returns>Human player count.</returns>
	public int GetHumanPlayerCount()
	{
		return Math.Min(NumPlayersInLobby, MaxPlayers);
	}

	/// <summary>
	/// Gets the number of AI players that would be added.
	/// </summary>
	/// <returns>AI player count.</returns>
	public int GetAIPlayerCount()
	{
		if (!WantAIplayers)
		{
			return 0;
		}

		var humanPlayers = GetHumanPlayerCount();
		var remainingSlots = MaxPlayers - humanPlayers;
		return Math.Max(0, remainingSlots);
	}

	/// <summary>
	/// Assigns a player to a team.
	/// </summary>
	/// <param name="playerID">Player to assign.</param>
	/// <param name="teamNumber">Team number.</param>
	public void AssignPlayerToTeam(PlayerID playerID, short teamNumber)
	{
		TeamNumbers[playerID] = teamNumber;
	}

	/// <summary>
	/// Sets a player's handicap.
	/// </summary>
	/// <param name="playerID">Player to set handicap for.</param>
	/// <param name="handicap">Handicap type.</param>
	public void SetPlayerHandicap(PlayerID playerID, HandicapType handicap)
	{
		Handicaps[playerID] = (short)handicap;
	}

	/// <summary>
	/// Gets a player's team assignment.
	/// </summary>
	/// <param name="playerID">Player to check.</param>
	/// <returns>Team number or -1 if not assigned.</returns>
	public short GetPlayerTeam(PlayerID playerID)
	{
		return TeamNumbers.TryGetValue(playerID, out var team) ? team : (short)-1;
	}

	/// <summary>
	/// Gets a player's handicap.
	/// </summary>
	/// <param name="playerID">Player to check.</param>
	/// <returns>Handicap type.</returns>
	public HandicapType GetPlayerHandicap(PlayerID playerID)
	{
		if (Handicaps.TryGetValue(playerID, out var handicap))
		{
			return (HandicapType)handicap;
		}

		return HandicapType.Normal;
	}

	/// <summary>
	/// Validates the lobby configuration.
	/// </summary>
	/// <returns>Collection of validation errors.</returns>
	public IReadOnlyCollection<string> Validate()
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(Name))
		{
			errors.Add("Lobby name is required");
		}

		if (MaxPlayers < 2 || MaxPlayers > 10)
		{
			errors.Add("Max players must be between 2 and 10");
		}

		if (InvalidHumanHQ.Length != MaxPlayers)
		{
			errors.Add($"InvalidHumanHQ array must have {MaxPlayers} elements");
		}

		if (GameOptions.Length == 0)
		{
			errors.Add("GameOptions array cannot be empty");
		}

		return errors;
	}

	/// <summary>
	/// Creates a deep copy of the lobby settings.
	/// </summary>
	/// <returns>New LobbySettings instance with copied values.</returns>
	public LobbySettings Copy()
	{
		var copy = new LobbySettings(MaxPlayers, KindOfLobby)
		{
			LobbyID = LobbyID,
			NumPlayersInLobby = NumPlayersInLobby,
			OwnerID = OwnerID,
			ServerGUID = ServerGUID,
			Password = Password,
			Name = Name,
			SteamVersionKey = SteamVersionKey,
			GameSetup = GameSetup,
			RulesSet = RulesSet,
			WantReplayFile = WantReplayFile,
			Location = Location,
			WantAIplayers = WantAIplayers,
			MapSizeIndex = MapSizeIndex,
			TerrainClassIndex = TerrainClassIndex,
			GameSpeedIndex = GameSpeedIndex,
			MapName = MapName,
			Seed = Seed,
			Latitude = Latitude,
			ResourceMinimum = ResourceMinimum,
			ResourcePresence = ResourcePresence,
			ColonyClass = ColonyClass,
		};

		// Deep copy arrays
		copy.InvalidHumanHQ = new bool[InvalidHumanHQ.Length];
		Array.Copy(InvalidHumanHQ, copy.InvalidHumanHQ, InvalidHumanHQ.Length);

		copy.GameOptions = new bool[GameOptions.Length];
		Array.Copy(GameOptions, copy.GameOptions, GameOptions.Length);

		// Deep copy dictionaries
		foreach (var kvp in TeamNumbers)
		{
			copy.TeamNumbers[kvp.Key] = kvp.Value;
		}

		foreach (var kvp in Handicaps)
		{
			copy.Handicaps[kvp.Key] = kvp.Value;
		}

		return copy;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return $"Lobby '{Name}' ({NumPlayersInLobby}/{MaxPlayers}) - {KindOfLobby} on {Location}";
	}
}
