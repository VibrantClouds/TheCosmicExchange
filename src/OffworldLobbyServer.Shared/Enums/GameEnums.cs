using System.ComponentModel;

namespace OffworldLobbyServer.Shared.Enums;

/// <summary>
/// Types of lobbies that can be created
/// Index 1 in SFSArray.
/// </summary>
public enum LobbyType : byte
{
	[Description("Multiplayer game")]
	Multiplayer = 0,

	[Description("Tournament match")]
	Tournament = 1,

	[Description("Custom match")]
	Custom = 2,

	[Description("Quick match")]
	QuickMatch = 3,
}

/// <summary>
/// Game setup configurations
/// Index 3 in SFSArray.
/// </summary>
public enum GameSetupType : short
{
	[Description("Standard setup")]
	Standard = 0,

	[Description("Custom setup")]
	Custom = 1,

	[Description("Beginner setup")]
	Beginner = 2,

	[Description("Advanced setup")]
	Advanced = 3,
}

/// <summary>
/// Rules set for the game
/// Index 4 in SFSArray.
/// </summary>
public enum RulesSetType : short
{
	[Description("Standard rules")]
	Standard = 0,

	[Description("Tournament rules")]
	Tournament = 1,

	[Description("Custom rules")]
	Custom = 2,

	[Description("Practice rules")]
	Practice = 3,
}

/// <summary>
/// Map locations available
/// Index 6 in SFSArray.
/// </summary>
public enum LocationType : short
{
	[Description("Mars")]
	Mars = 0,

	[Description("Europa")]
	Europa = 1,

	[Description("Asteroid Belt")]
	AsteroidBelt = 2,

	[Description("Titan")]
	Titan = 3,

	[Description("Earth")]
	Earth = 4,
}

/// <summary>
/// Map size options
/// Index 9 in SFSArray.
/// </summary>
public enum MapSizeType : byte
{
	[Description("Small")]
	Small = 0,

	[Description("Medium")]
	Medium = 1,

	[Description("Large")]
	Large = 2,

	[Description("Extra Large")]
	ExtraLarge = 3,
}

/// <summary>
/// Terrain class types
/// Index 10 in SFSArray.
/// </summary>
public enum TerrainClassType : short
{
	[Description("Open")]
	Open = 0,

	[Description("Dense")]
	Dense = 1,

	[Description("Scattered")]
	Scattered = 2,

	[Description("Complex")]
	Complex = 3,

	[Description("Random")]
	Random = 4,
}

/// <summary>
/// Game speed settings
/// Index 11 in SFSArray.
/// </summary>
public enum GameSpeedType : byte
{
	[Description("Very Slow")]
	VerySlow = 0,

	[Description("Slow")]
	Slow = 1,

	[Description("Normal")]
	Normal = 2,

	[Description("Fast")]
	Fast = 3,

	[Description("Very Fast")]
	VeryFast = 4,
}

/// <summary>
/// Map latitude settings
/// Index 14 in SFSArray.
/// </summary>
public enum LatitudeType : short
{
	[Description("Equatorial")]
	Equatorial = 0,

	[Description("Tropical")]
	Tropical = 1,

	[Description("Temperate")]
	Temperate = 2,

	[Description("Polar")]
	Polar = 3,

	[Description("Random")]
	Random = 4,
}

/// <summary>
/// Resource minimum settings
/// Index 15 in SFSArray.
/// </summary>
public enum ResourceMinimumType : byte
{
	[Description("Very Low")]
	VeryLow = 0,

	[Description("Low")]
	Low = 1,

	[Description("Normal")]
	Normal = 2,

	[Description("High")]
	High = 3,

	[Description("Very High")]
	VeryHigh = 4,
}

/// <summary>
/// Resource presence settings
/// Index 16 in SFSArray.
/// </summary>
public enum ResourcePresenceType : byte
{
	[Description("Scarce")]
	Scarce = 0,

	[Description("Limited")]
	Limited = 1,

	[Description("Normal")]
	Normal = 2,

	[Description("Abundant")]
	Abundant = 3,

	[Description("Unlimited")]
	Unlimited = 4,
}

/// <summary>
/// Colony class types
/// Index 17 in SFSArray.
/// </summary>
public enum ColonyClassType : short
{
	[Description("Mining")]
	Mining = 0,

	[Description("Industrial")]
	Industrial = 1,

	[Description("Trade")]
	Trade = 2,

	[Description("Research")]
	Research = 3,

	[Description("Military")]
	Military = 4,

	[Description("Random")]
	Random = 5,
}

/// <summary>
/// Player handicap types used in game.
/// </summary>
public enum HandicapType : short
{
	[Description("None")]
	None = -1,

	[Description("Observer")]
	Observer = 0,

	[Description("Beginner")]
	Beginner = 1,

	[Description("Easy")]
	Easy = 2,

	[Description("Normal")]
	Normal = 3,

	[Description("Hard")]
	Hard = 4,

	[Description("Expert")]
	Expert = 5,
}

/// <summary>
/// Player gender options.
/// </summary>
public enum GenderType
{
	[Description("Male")]
	Male = 0,

	[Description("Female")]
	Female = 1,

	[Description("Other")]
	Other = 2,
}

/// <summary>
/// Storefront provider identification.
/// </summary>
public enum StorefrontId
{
	None = 0,
	Steam = 1,
	EpicGames = 2,
	GOG = 3,
	Xbox = 4,
	PlayStation = 5,
}
