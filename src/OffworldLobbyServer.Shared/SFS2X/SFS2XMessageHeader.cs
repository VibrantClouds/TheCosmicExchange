namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// Represents the SFS2X message header according to confirmed protocol specification.
/// Format: [Controller:1][Action:1][Length:4][SFSObject payload]
/// Source: docs/ReverseEngineeringNotes.md:41-46
/// </summary>
public class SFS2XMessageHeader
{
	/// <summary>
	/// Controller ID (0x00=System, 0x01=Extension).
	/// </summary>
	public byte Controller { get; set; }

	/// <summary>
	/// Action ID within the controller.
	/// </summary>
	public byte Action { get; set; }

	/// <summary>
	/// Length of the SFSObject payload in bytes.
	/// </summary>
	public uint Length { get; set; }

	/// <summary>
	/// Offset in the message data where the payload starts.
	/// Always 6 bytes from start (Controller + Action + Length).
	/// </summary>
	public int PayloadOffset => 6;

	/// <summary>
	/// Total message size including header.
	/// </summary>
	public int TotalSize => PayloadOffset + (int)Length;

	/// <summary>
	/// Creates a new message header.
	/// </summary>
	/// <param name="controller">Controller ID.</param>
	/// <param name="action">Action ID.</param>
	/// <param name="length">Payload length.</param>
	public SFS2XMessageHeader(byte controller, byte action, uint length)
	{
		Controller = controller;
		Action = action;
		Length = length;
	}

	/// <summary>
	/// Serializes the header to binary format.
	/// </summary>
	/// <returns>6-byte header array.</returns>
	public byte[] ToBytes()
	{
		var header = new byte[6];
		header[0] = Controller;
		header[1] = Action;
		
		// Length as big-endian 32-bit integer
		header[2] = (byte)(Length >> 24);
		header[3] = (byte)(Length >> 16);
		header[4] = (byte)(Length >> 8);
		header[5] = (byte)(Length);
		
		return header;
	}

	/// <summary>
	/// Parses a header from binary data.
	/// </summary>
	/// <param name="data">Message data (must be at least 6 bytes).</param>
	/// <returns>Parsed header.</returns>
	/// <exception cref="ArgumentException">Invalid data length.</exception>
	public static SFS2XMessageHeader FromBytes(byte[] data)
	{
		if (data.Length < 6)
		{
			throw new ArgumentException($"Invalid header data length: {data.Length} (expected at least 6 bytes)");
		}

		var controller = data[0];
		var action = data[1];
		
		// Parse length as big-endian 32-bit integer
		var length = (uint)((data[2] << 24) | (data[3] << 16) | (data[4] << 8) | data[5]);
		
		return new SFS2XMessageHeader(controller, action, length);
	}

	public override string ToString()
	{
		return $"SFS2X Header - Controller: 0x{Controller:X2}, Action: 0x{Action:X2}, Length: {Length} bytes";
	}
}