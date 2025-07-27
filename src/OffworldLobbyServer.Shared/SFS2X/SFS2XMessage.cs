namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// Represents a complete SFS2X message with header and payload.
/// Follows confirmed protocol specification: [Controller][Action][Length][SFSObject]
/// </summary>
public class SFS2XMessage
{
	/// <summary>
	/// Message header containing controller, action, and length.
	/// </summary>
	public SFS2XMessageHeader Header { get; }

	/// <summary>
	/// Message payload as SFSObject.
	/// </summary>
	public SFSObject Payload { get; }

	/// <summary>
	/// Creates a new SFS2X message.
	/// </summary>
	/// <param name="controller">Controller ID.</param>
	/// <param name="action">Action ID.</param>
	/// <param name="payload">SFSObject payload.</param>
	public SFS2XMessage(byte controller, byte action, SFSObject payload)
	{
		Payload = payload ?? throw new ArgumentNullException(nameof(payload));
		
		// Serialize payload to get length
		var payloadBytes = SFS2XSerializer.SerializeObject(payload);
		Header = new SFS2XMessageHeader(controller, action, (uint)payloadBytes.Length);
	}

	/// <summary>
	/// Creates a message from existing header and payload.
	/// </summary>
	/// <param name="header">Pre-built header.</param>
	/// <param name="payload">SFSObject payload.</param>
	public SFS2XMessage(SFS2XMessageHeader header, SFSObject payload)
	{
		Header = header ?? throw new ArgumentNullException(nameof(header));
		Payload = payload ?? throw new ArgumentNullException(nameof(payload));
	}

	/// <summary>
	/// Serializes the complete message to binary format.
	/// </summary>
	/// <returns>Complete message bytes.</returns>
	public byte[] ToBytes()
	{
		var headerBytes = Header.ToBytes();
		var payloadBytes = SFS2XSerializer.SerializeObject(Payload);
		
		var message = new byte[headerBytes.Length + payloadBytes.Length];
		Array.Copy(headerBytes, 0, message, 0, headerBytes.Length);
		Array.Copy(payloadBytes, 0, message, headerBytes.Length, payloadBytes.Length);
		
		return message;
	}

	/// <summary>
	/// Parses a complete message from binary data.
	/// </summary>
	/// <param name="data">Complete message bytes.</param>
	/// <returns>Parsed SFS2X message.</returns>
	/// <exception cref="ArgumentException">Invalid message format.</exception>
	public static SFS2XMessage FromBytes(byte[] data)
	{
		if (data.Length < 6)
		{
			throw new ArgumentException($"Invalid message length: {data.Length} (minimum 6 bytes for header)");
		}

		// Parse header
		var header = SFS2XMessageHeader.FromBytes(data);
		
		// Validate payload length
		if (data.Length < header.TotalSize)
		{
			throw new ArgumentException(
				$"Message too short: {data.Length} bytes (expected {header.TotalSize} based on header)");
		}

		// Extract payload
		var payloadBytes = new byte[header.Length];
		Array.Copy(data, header.PayloadOffset, payloadBytes, 0, (int)header.Length);
		
		// Deserialize payload
		var payload = SFS2XSerializer.DeserializeObject(payloadBytes);
		
		return new SFS2XMessage(header, payload);
	}

	/// <summary>
	/// Converts message to base64 string for BlueBox transport.
	/// </summary>
	/// <returns>Base64-encoded message.</returns>
	public string ToBase64()
	{
		return Convert.ToBase64String(ToBytes());
	}

	/// <summary>
	/// Parses message from base64 string from BlueBox transport.
	/// </summary>
	/// <param name="base64Data">Base64-encoded message.</param>
	/// <returns>Parsed SFS2X message.</returns>
	public static SFS2XMessage FromBase64(string base64Data)
	{
		var bytes = Convert.FromBase64String(base64Data);
		return FromBytes(bytes);
	}

	public override string ToString()
	{
		return $"SFS2X Message - {Header}, Payload: {Payload.Size} entries";
	}
}