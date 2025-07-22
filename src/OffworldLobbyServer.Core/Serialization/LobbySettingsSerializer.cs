using OffworldLobbyServer.Core.Models;
using OffworldLobbyServer.Shared.Enums;
using OffworldLobbyServer.Shared.Models;
using OffworldLobbyServer.Shared.SFS2X;

namespace OffworldLobbyServer.Core.Serialization;

/// <summary>
/// Converts LobbySettings to/from 21-element SFSArray format
/// Matches the exact structure used by the game's AmazonManager.
/// </summary>
public class LobbySettingsSerializer
{
	/// <summary>
	/// Converts LobbySettings to SFSArray with the exact 21-element structure.
	/// </summary>
	/// <param name="settings">Lobby settings to serialize.</param>
	/// <returns>21-element SFSArray.</returns>
	public static SFSArray ToSFSArray(LobbySettings settings)
	{
		var array = new SFSArray();

		// Index 0: Lobby name
		array.AddUtfString(settings.Name);

		// Index 1: Lobby type
		array.AddByte((byte)settings.KindOfLobby);

		// Index 2: Steam version key
		array.AddUtfString(settings.SteamVersionKey);

		// Index 3: Game setup
		array.AddShort((short)settings.GameSetup);

		// Index 4: Rules set
		array.AddShort((short)settings.RulesSet);

		// Index 5: Want replay file
		array.AddBool(settings.WantReplayFile);

		// Index 6: Location
		array.AddShort((short)settings.Location);

		// Index 7: Invalid human HQ array
		array.AddBoolArray(settings.InvalidHumanHQ);

		// Index 8: Want AI players
		array.AddBool(settings.WantAIplayers);

		// Index 9: Map size
		array.AddByte((byte)settings.MapSizeIndex);

		// Index 10: Terrain class
		array.AddShort((short)settings.TerrainClassIndex);

		// Index 11: Game speed
		array.AddByte((byte)settings.GameSpeedIndex);

		// Index 12: Map name
		array.AddUtfString(settings.MapName);

		// Index 13: Seed
		array.AddInt(settings.Seed);

		// Index 14: Latitude
		array.AddShort((short)settings.Latitude);

		// Index 15: Resource minimum
		array.AddByte((byte)settings.ResourceMinimum);

		// Index 16: Resource presence
		array.AddByte((byte)settings.ResourcePresence);

		// Index 17: Colony class
		array.AddShort((short)settings.ColonyClass);

		// Index 18: Game options array
		array.AddBoolArray(settings.GameOptions);

		// Index 19: Team assignments (SFSObject: PlayerID -> team number)
		var teamAssignments = new SFSObject();
		foreach (var assignment in settings.TeamNumbers)
		{
			teamAssignments.PutShort(assignment.Key.ToString(), assignment.Value);
		}

		array.AddSFSObject(teamAssignments);

		// Index 20: Handicap assignments (SFSObject: PlayerID -> handicap value)
		var handicapAssignments = new SFSObject();
		foreach (var assignment in settings.Handicaps)
		{
			handicapAssignments.PutShort(assignment.Key.ToString(), assignment.Value);
		}

		array.AddSFSObject(handicapAssignments);

		return array;
	}

	/// <summary>
	/// Converts 21-element SFSArray to LobbySettings.
	/// </summary>
	/// <param name="array">21-element SFSArray.</param>
	/// <param name="maxPlayers">Maximum players for the lobby.</param>
	/// <param name="lobbyID">Lobby identifier.</param>
	/// <param name="name">Lobby name.</param>
	/// <param name="ownerID">Owner player ID.</param>
	/// <param name="guid">Unity network GUID.</param>
	/// <param name="numPlayers">Current number of players.</param>
	/// <returns>Deserialized LobbySettings.</returns>
	public static LobbySettings FromSFSArray(SFSArray array, int maxPlayers = 10, int lobbyID = 0,
		string name = "", string ownerID = "", string guid = "", int numPlayers = 0)
	{
		if (array.Size < 21)
		{
			throw new ArgumentException($"SFSArray must have at least 21 elements, got {array.Size}");
		}

		var settings = new LobbySettings(maxPlayers, LobbyType.Multiplayer)
		{
			LobbyID = lobbyID,
			NumPlayersInLobby = numPlayers,
			ServerGUID = guid,
		};

		// Set owner if provided
		if (!string.IsNullOrEmpty(ownerID))
		{
			settings.OwnerID = PlayerID.FromString(ownerID);
		}

		// Deserialize from 21-element array
		settings.Name = array.GetUtfString(0);
		settings.KindOfLobby = (LobbyType)array.GetByte(1);
		settings.SteamVersionKey = array.GetUtfString(2);
		settings.GameSetup = (GameSetupType)array.GetShort(3);
		settings.RulesSet = (RulesSetType)array.GetShort(4);
		settings.WantReplayFile = array.GetBool(5);
		settings.Location = (LocationType)array.GetShort(6);
		settings.InvalidHumanHQ = array.GetBoolArray(7);
		settings.WantAIplayers = array.GetBool(8);
		settings.MapSizeIndex = (MapSizeType)array.GetByte(9);
		settings.TerrainClassIndex = (TerrainClassType)array.GetShort(10);
		settings.GameSpeedIndex = (GameSpeedType)array.GetByte(11);
		settings.MapName = array.GetUtfString(12);
		settings.Seed = array.GetInt(13);
		settings.Latitude = (LatitudeType)array.GetShort(14);
		settings.ResourceMinimum = (ResourceMinimumType)array.GetByte(15);
		settings.ResourcePresence = (ResourcePresenceType)array.GetByte(16);
		settings.ColonyClass = (ColonyClassType)array.GetShort(17);
		settings.GameOptions = array.GetBoolArray(18);

		// Deserialize team assignments (Index 19)
		var teamAssignments = array.GetSFSObject(19);
		foreach (var playerIDString in teamAssignments.GetKeys())
		{
			var playerID = PlayerID.FromString(playerIDString);
			var teamNumber = teamAssignments.GetShort(playerIDString);
			settings.TeamNumbers[playerID] = teamNumber;
		}

		// Deserialize handicap assignments (Index 20)
		var handicapAssignments = array.GetSFSObject(20);
		foreach (var playerIDString in handicapAssignments.GetKeys())
		{
			var playerID = PlayerID.FromString(playerIDString);
			var handicapValue = handicapAssignments.GetShort(playerIDString);
			settings.Handicaps[playerID] = handicapValue;
		}

		return settings;
	}

	/// <summary>
	/// Serializes LobbySettings to binary using SFSArray format.
	/// </summary>
	/// <param name="settings">Lobby settings to serialize.</param>
	/// <param name="writer">Binary writer.</param>
	public static void Serialize(LobbySettings settings, BinaryWriter writer)
	{
		var array = ToSFSArray(settings);
		array.Serialize(writer);
	}

	/// <summary>
	/// Deserializes LobbySettings from binary SFSArray format.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="maxPlayers">Maximum players for the lobby.</param>
	/// <param name="lobbyID">Lobby identifier.</param>
	/// <param name="name">Lobby name.</param>
	/// <param name="ownerID">Owner player ID.</param>
	/// <param name="guid">Unity network GUID.</param>
	/// <param name="numPlayers">Current number of players.</param>
	/// <returns>Deserialized LobbySettings.</returns>
	public static LobbySettings Deserialize(BinaryReader reader, int maxPlayers = 10, int lobbyID = 0,
		string name = "", string ownerID = "", string guid = "", int numPlayers = 0)
	{
		var array = SFSArray.Deserialize(reader);
		return FromSFSArray(array, maxPlayers, lobbyID, name, ownerID, guid, numPlayers);
	}

	/// <summary>
	/// Creates a default 21-element SFSArray for testing.
	/// </summary>
	/// <param name="lobbyName">Name for the lobby.</param>
	/// <returns>Default lobby settings as SFSArray.</returns>
	public static SFSArray CreateDefaultSFSArray(string lobbyName = "Test Lobby")
	{
		var defaultSettings = new LobbySettings(10, LobbyType.Multiplayer)
		{
			Name = lobbyName,
			KindOfLobby = LobbyType.Multiplayer,
			SteamVersionKey = "1.0.0",
			GameSetup = GameSetupType.Standard,
			RulesSet = RulesSetType.Standard,
			WantReplayFile = false,
			Location = LocationType.Mars,
			InvalidHumanHQ = new bool[10],
			WantAIplayers = true,
			MapSizeIndex = MapSizeType.Medium,
			TerrainClassIndex = TerrainClassType.Open,
			GameSpeedIndex = GameSpeedType.Normal,
			MapName = string.Empty,
			Seed = 12345,
			Latitude = LatitudeType.Equatorial,
			ResourceMinimum = ResourceMinimumType.Normal,
			ResourcePresence = ResourcePresenceType.Normal,
			ColonyClass = ColonyClassType.Mining,
			GameOptions = new bool[32],
		};

		return ToSFSArray(defaultSettings);
	}

	/// <summary>
	/// Validates that an SFSArray has the correct 21-element structure.
	/// </summary>
	/// <param name="array">Array to validate.</param>
	/// <returns>List of validation errors, empty if valid.</returns>
	public static List<string> ValidateSFSArray(SFSArray array)
	{
		var errors = new List<string>();

		if (array.Size != 21)
		{
			errors.Add($"Array must have exactly 21 elements, got {array.Size}");
			return errors; // Can't validate structure if wrong size
		}

		try
		{
			// Validate each element type
			array.GetUtfString(0);  // Name
			array.GetByte(1);       // Lobby type
			array.GetUtfString(2);  // Version key
			array.GetShort(3);      // Game setup
			array.GetShort(4);      // Rules set
			array.GetBool(5);       // Want replay
			array.GetShort(6);      // Location
			array.GetBoolArray(7);  // Invalid HQ
			array.GetBool(8);       // Want AI
			array.GetByte(9);       // Map size
			array.GetShort(10);     // Terrain class
			array.GetByte(11);      // Game speed
			array.GetUtfString(12); // Map name
			array.GetInt(13);       // Seed
			array.GetShort(14);     // Latitude
			array.GetByte(15);      // Resource min
			array.GetByte(16);      // Resource presence
			array.GetShort(17);     // Colony class
			array.GetBoolArray(18); // Game options
			array.GetSFSObject(19); // Team assignments
			array.GetSFSObject(20); // Handicap assignments
		}
		catch (InvalidCastException ex)
		{
			errors.Add($"Type validation failed: {ex.Message}");
		}

		return errors;
	}
}
