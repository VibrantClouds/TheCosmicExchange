using OffworldLobbyServer.Shared.Enums;

namespace OffworldLobbyServer.Shared.Models;

/// <summary>
/// Combined player identification with network information
/// Matches the game's CombinedID structure and serialization.
/// </summary>
public class CombinedID
{
	/// <summary>
	/// Player identification information.
	/// </summary>
	public PlayerID PlayerID { get; set; } = new ();

	/// <summary>
	/// IP address for network connection.
	/// </summary>
	public string IPAddress { get; set; } = string.Empty;

	/// <summary>
	/// Port for network connection.
	/// </summary>
	public int Port { get; set; }

	/// <summary>
	/// Provider ID for special identification (e.g., "Server").
	/// </summary>
	public string ProviderId { get; set; } = string.Empty;

	/// <summary>
	/// Creates a new CombinedID instance.
	/// </summary>
	public CombinedID()
	{
	}

	/// <summary>
	/// Creates a CombinedID with IP and port.
	/// </summary>
	/// <param name="ipAddress">IP address.</param>
	/// <param name="port">Port number.</param>
	public CombinedID(string ipAddress, int port)
	{
		IPAddress = ipAddress ?? string.Empty;
		Port = port;
	}

	/// <summary>
	/// Creates a CombinedID with provider ID, storefront, IP and port
	/// Used for server identification.
	/// </summary>
	/// <param name="providerId">Provider identifier (e.g., "Server").</param>
	/// <param name="storefront">Storefront ID.</param>
	/// <param name="ipAddress">IP address.</param>
	/// <param name="port">Port number.</param>
	public CombinedID(string providerId, StorefrontId storefront, string ipAddress, int port)
	{
		ProviderId = providerId ?? string.Empty;
		PlayerID = new PlayerID(storefront, string.Empty);
		IPAddress = ipAddress ?? string.Empty;
		Port = port;
	}

	/// <summary>
	/// Creates a CombinedID with complete player information.
	/// </summary>
	/// <param name="playerID">Player identification.</param>
	/// <param name="ipAddress">IP address.</param>
	/// <param name="port">Port number.</param>
	public CombinedID(PlayerID playerID, string ipAddress, int port)
	{
		PlayerID = playerID ?? new PlayerID();
		IPAddress = ipAddress ?? string.Empty;
		Port = port;
	}

	/// <summary>
	/// Creates an invalid CombinedID instance.
	/// </summary>
	/// <returns>Invalid CombinedID.</returns>
	public static CombinedID CreateInvalidID()
	{
		return new CombinedID();
	}

	/// <summary>
	/// Checks if this ID represents a valid network connection.
	/// </summary>
	/// <returns>True if valid.</returns>
	public bool IsValid()
	{
		return !string.IsNullOrEmpty(IPAddress) && Port > 0;
	}

	/// <summary>
	/// Checks if this ID has a valid IP address.
	/// </summary>
	/// <returns>True if IP is valid.</returns>
	public bool ValidIP()
	{
		return !string.IsNullOrEmpty(IPAddress) && IPAddress != "0.0.0.0";
	}

	/// <summary>
	/// Get player ID string representation.
	/// </summary>
	/// <returns>Player ID string.</returns>
	public string GetPlayerID()
	{
		return PlayerID?.ToString() ?? string.Empty;
	}

	/// <summary>
	/// Serialize CombinedID to binary stream
	/// Matches game's CombinedID.Serialize method exactly:
	/// PlayerID.Serialize(stream, ref value.playerID);
	/// SimplifyIO.Data(stream, ref value.mIPaddress, "IP address");
	/// SimplifyIO.Data(stream, ref value.mPort, "Port");.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	public void Serialize(BinaryWriter writer)
	{
		// Serialize PlayerID
		PlayerID.Serialize(writer);

		// Write IP address with SimplifyIO-style key
		WriteSimplifyIOString(writer, IPAddress, "IP address");

		// Write Port with SimplifyIO-style key
		WriteSimplifyIOInt(writer, Port, "Port");
	}

	/// <summary>
	/// Deserialize CombinedID from binary stream.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized CombinedID.</returns>
	public static CombinedID Deserialize(BinaryReader reader)
	{
		var playerID = PlayerID.Deserialize(reader);
		var ipAddress = ReadSimplifyIOString(reader, "IP address");
		var port = ReadSimplifyIOInt(reader, "Port");

		return new CombinedID(playerID, ipAddress, port);
	}

	/// <summary>
	/// Write string with SimplifyIO-style key (simplified version).
	/// </summary>
	private static void WriteSimplifyIOString(BinaryWriter writer, string value, string key)
	{
		// Simplified implementation - in real game this would be more complex
		writer.Write(key);
		writer.Write(value ?? string.Empty);
	}

	/// <summary>
	/// Write integer with SimplifyIO-style key (simplified version).
	/// </summary>
	private static void WriteSimplifyIOInt(BinaryWriter writer, int value, string key)
	{
		// Simplified implementation - in real game this would be more complex
		writer.Write(key);
		writer.Write(value);
	}

	/// <summary>
	/// Read string with SimplifyIO-style key (simplified version).
	/// </summary>
	private static string ReadSimplifyIOString(BinaryReader reader, string expectedKey)
	{
		var key = reader.ReadString();
		if (key != expectedKey)
		{
			throw new InvalidDataException($"Expected key '{expectedKey}' but got '{key}'");
		}

		return reader.ReadString();
	}

	/// <summary>
	/// Read integer with SimplifyIO-style key (simplified version).
	/// </summary>
	private static int ReadSimplifyIOInt(BinaryReader reader, string expectedKey)
	{
		var key = reader.ReadString();
		if (key != expectedKey)
		{
			throw new InvalidDataException($"Expected key '{expectedKey}' but got '{key}'");
		}

		return reader.ReadInt32();
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		if (!string.IsNullOrEmpty(ProviderId))
		{
			return $"{ProviderId}@{IPAddress}:{Port}";
		}

		return $"{PlayerID}@{IPAddress}:{Port}";
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is CombinedID other)
		{
			return PlayerID.Equals(other.PlayerID) &&
				   IPAddress == other.IPAddress &&
				   Port == other.Port &&
				   ProviderId == other.ProviderId;
		}

		return false;
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return HashCode.Combine(PlayerID, IPAddress, Port, ProviderId);
	}
}
