namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// Represents the SFS2X message header according to confirmed protocol specification.
/// Supports two formats:
/// - Standard: [Protocol:1][0x00][0x00][Length:1][Controller:1][Action:1][Payload]
/// - Compact: [Protocol:1][Length:2 big-endian][Payload]
/// </summary>
public class SFS2XMessageHeader
{
	/// <summary>
	/// Protocol header byte (0x80=System messages, 0x90=Extension messages).
	/// </summary>
	public byte ProtocolHeader { get; set; }

	/// <summary>
	/// Controller ID (0x00=System, 0x01=Extension).
	/// May be embedded in payload for compact format.
	/// </summary>
	public byte Controller { get; set; }

	/// <summary>
	/// Action ID within the controller.
	/// May be embedded in payload for compact format.
	/// </summary>
	public byte Action { get; set; }

	/// <summary>
	/// Length of the payload in bytes.
	/// </summary>
	public uint Length { get; set; }

	/// <summary>
	/// Whether this header uses compact format (3 bytes) or standard format (6 bytes).
	/// </summary>
	public bool IsCompactFormat { get; set; }

	/// <summary>
	/// Offset in the message data where the payload starts.
	/// Compact format: 3 bytes, Standard format: 6 bytes.
	/// </summary>
	public int PayloadOffset => IsCompactFormat ? 3 : 6;

	/// <summary>
	/// Total message size including header.
	/// </summary>
	public int TotalSize => PayloadOffset + (int)Length;

	/// <summary>
	/// Creates a new message header.
	/// </summary>
	/// <param name="protocolHeader">Protocol header byte.</param>
	/// <param name="controller">Controller ID.</param>
	/// <param name="action">Action ID.</param>
	/// <param name="length">Payload length.</param>
	public SFS2XMessageHeader(byte protocolHeader, byte controller, byte action, uint length)
	{
		ProtocolHeader = protocolHeader;
		Controller = controller;
		Action = action;
		Length = length;
	}

	/// <summary>
	/// Creates a new message header, automatically determining protocol header from controller.
	/// </summary>
	/// <param name="controller">Controller ID.</param>
	/// <param name="action">Action ID.</param>
	/// <param name="length">Payload length.</param>
	public SFS2XMessageHeader(byte controller, byte action, uint length)
	{
		// Automatically set protocol header based on controller type
		ProtocolHeader = controller == 0x00 ? (byte)0x80 : (byte)0x90;
		Controller = controller;
		Action = action;
		Length = length;
	}

	/// <summary>
	/// Serializes the header to binary format.
	/// </summary>
	/// <returns>Header byte array (3 or 6 bytes depending on format).</returns>
	public byte[] ToBytes()
	{
		if (IsCompactFormat)
		{
			// Compact format: [Protocol][Length:2 bytes big-endian]
			var header = new byte[3];
			header[0] = ProtocolHeader;
			header[1] = (byte)(Length >> 8);
			header[2] = (byte)(Length);
			return header;
		}
		else
		{
			// Standard format: [Protocol][0x00][0x00][Length:1][Controller][Action]
			var header = new byte[6];
			header[0] = ProtocolHeader;
			header[1] = 0x00;
			header[2] = 0x00;
			header[3] = (byte)(Length & 0xFF); // Length limited to 255 in standard format
			header[4] = Controller;
			header[5] = Action;
			return header;
		}
	}

	/// <summary>
	/// Parses a header from binary data.
	/// Detects format based on header pattern.
	/// </summary>
	/// <param name="data">Message data (must be at least 3 bytes).</param>
	/// <returns>Parsed header.</returns>
	/// <exception cref="ArgumentException">Invalid data length.</exception>
	public static SFS2XMessageHeader FromBytes(byte[] data)
	{
		if (data.Length < 3)
		{
			throw new ArgumentException($"Invalid header data length: {data.Length} (expected at least 3 bytes)");
		}

		var protocolHeader = data[0];
		
		// Detect format: Standard format has two consecutive 0x00 bytes after protocol header
		bool isStandardFormat = data.Length >= 3 && data[1] == 0x00 && data[2] == 0x00;
		
		if (!isStandardFormat)
		{
			// Compact format: [Protocol][Length:2 bytes big-endian]
			if (data.Length < 3)
			{
				throw new ArgumentException("Compact format requires at least 3 bytes");
			}
			
			var length = (uint)((data[1] << 8) | data[2]);
			
			// Validate length makes sense for compact format
			if (length > data.Length - 3)
			{
				throw new ArgumentException(
					$"Compact format length mismatch: header indicates {length} bytes but only {data.Length - 3} available");
			}
			
			return new SFS2XMessageHeader(protocolHeader, 0xFF, 0xFF, length)
			{
				IsCompactFormat = true
			};
		}
		else
		{
			// Standard format: [Protocol][0x00][0x00][Length:1][Controller][Action]
			if (data.Length < 6)
			{
				throw new ArgumentException($"Standard format requires at least 6 bytes, got {data.Length}");
			}
			
			var length = (uint)data[3];
			var controller = data[4];
			var action = data[5];
			
			// Validate length makes sense for standard format
			if (length > data.Length - 6)
			{
				throw new ArgumentException(
					$"Standard format length mismatch: header indicates {length} bytes but only {data.Length - 6} available");
			}
			
			return new SFS2XMessageHeader(protocolHeader, controller, action, length)
			{
				IsCompactFormat = false
			};
		}
	}

	public override string ToString()
	{
		return $"SFS2X Header - Protocol: 0x{ProtocolHeader:X2}, Format: {(IsCompactFormat ? "Compact" : "Standard")}, Controller: 0x{Controller:X2}, Action: 0x{Action:X2}, Length: {Length} bytes";
	}
}