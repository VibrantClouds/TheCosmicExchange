using System.ComponentModel;
using OffworldLobbyServer.Shared.Enums;

namespace OffworldLobbyServer.Shared.Models;

/// <summary>
/// Represents a player identifier with storefront information
/// Used as part of CombinedID serialization.
/// </summary>
public class PlayerID
{
	/// <summary>
	/// Storefront provider (Steam, Epic, GOG, etc.)
	/// </summary>
	public StorefrontId Storefront { get; set; } = StorefrontId.None;

	/// <summary>
	/// Unique player identifier string from the storefront.
	/// </summary>
	public string PlayerId { get; set; } = string.Empty;

	/// <summary>
	/// Player display name.
	/// </summary>
	public string DisplayName { get; set; } = string.Empty;

	/// <summary>
	/// Creates a new PlayerID instance.
	/// </summary>
	public PlayerID()
	{
	}

	/// <summary>
	/// Creates a new PlayerID with specified values.
	/// </summary>
	/// <param name="storefront">The storefront provider.</param>
	/// <param name="playerId">The unique player ID.</param>
	/// <param name="displayName">The display name.</param>
	public PlayerID(StorefrontId storefront, string playerId, string displayName = "")
	{
		Storefront = storefront;
		PlayerId = playerId ?? string.Empty;
		DisplayName = displayName ?? string.Empty;
	}

	/// <summary>
	/// Parse a PlayerID from string representation
	/// Expected format: "storefront:playerId" or just "playerId".
	/// </summary>
	/// <param name="playerIdString">String representation of player ID.</param>
	/// <returns>Parsed PlayerID instance.</returns>
	public static PlayerID FromString(string playerIdString)
	{
		if (string.IsNullOrEmpty(playerIdString))
		{
			return new PlayerID();
		}

		var parts = playerIdString.Split(':', 2);
		if (parts.Length == 2)
		{
			// Format: "storefront:playerId"
			if (Enum.TryParse<StorefrontId>(parts[0], true, out var storefront))
			{
				return new PlayerID(storefront, parts[1]);
			}
		}

		// Default to treating entire string as player ID
		return new PlayerID(StorefrontId.None, playerIdString);
	}

	/// <summary>
	/// Convert PlayerID to string representation.
	/// </summary>
	/// <returns>String representation.</returns>
	public override string ToString()
	{
		if (Storefront == StorefrontId.None)
		{
			return PlayerId;
		}

		return $"{Storefront}:{PlayerId}";
	}

	/// <summary>
	/// Serialize PlayerID to binary stream
	/// Implementation matches game's PlayerID.Serialize method.
	/// </summary>
	/// <param name="writer">Binary writer to write to.</param>
	public void Serialize(BinaryWriter writer)
	{
		// Write storefront ID
		writer.Write((int)Storefront);

		// Write player ID string
		writer.Write(PlayerId ?? string.Empty);

		// Write display name
		writer.Write(DisplayName ?? string.Empty);
	}

	/// <summary>
	/// Deserialize PlayerID from binary stream.
	/// </summary>
	/// <param name="reader">Binary reader to read from.</param>
	/// <returns>Deserialized PlayerID.</returns>
	public static PlayerID Deserialize(BinaryReader reader)
	{
		var storefront = (StorefrontId)reader.ReadInt32();
		var playerId = reader.ReadString();
		var displayName = reader.ReadString();

		return new PlayerID(storefront, playerId, displayName);
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is PlayerID other)
		{
			return Storefront == other.Storefront && PlayerId == other.PlayerId;
		}

		return false;
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return HashCode.Combine(Storefront, PlayerId);
	}
}
