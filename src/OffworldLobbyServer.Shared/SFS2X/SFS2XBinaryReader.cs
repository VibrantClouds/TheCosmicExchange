using System.Text;

namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// Binary reader for SFS2X protocol data.
/// Implements the exact binary format expected by SmartFoxServer 2X clients.
/// </summary>
public class SFS2XBinaryReader : IDisposable
{
	private readonly Stream _stream;
	private readonly BinaryReader _reader;
	private bool _disposed = false;

	/// <summary>
	/// Initializes a new instance of the SFS2XBinaryReader class.
	/// </summary>
	/// <param name="data">Byte array to read from.</param>
	public SFS2XBinaryReader(byte[] data)
		: this(new MemoryStream(data))
	{
	}

	/// <summary>
	/// Initializes a new instance of the SFS2XBinaryReader class.
	/// </summary>
	/// <param name="stream">Stream to read from.</param>
	public SFS2XBinaryReader(Stream stream)
	{
		_stream = stream;
		_reader = new BinaryReader(stream, Encoding.UTF8);
	}

	/// <summary>
	/// Reads a single byte.
	/// </summary>
	/// <returns>Byte value.</returns>
	public byte ReadByte()
	{
		return _reader.ReadByte();
	}

	/// <summary>
	/// Reads a boolean value (byte 0 or 1).
	/// </summary>
	/// <returns>Boolean value.</returns>
	public bool ReadBool()
	{
		return _reader.ReadByte() != 0;
	}

	/// <summary>
	/// Reads a short integer in big-endian format.
	/// </summary>
	/// <returns>Short value.</returns>
	public short ReadShort()
	{
		var bytes = _reader.ReadBytes(2);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return BitConverter.ToInt16(bytes, 0);
	}

	/// <summary>
	/// Reads an integer in big-endian format.
	/// </summary>
	/// <returns>Integer value.</returns>
	public int ReadInt()
	{
		var bytes = _reader.ReadBytes(4);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return BitConverter.ToInt32(bytes, 0);
	}

	/// <summary>
	/// Reads a long integer in big-endian format.
	/// </summary>
	/// <returns>Long value.</returns>
	public long ReadLong()
	{
		var bytes = _reader.ReadBytes(8);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return BitConverter.ToInt64(bytes, 0);
	}

	/// <summary>
	/// Reads a float in big-endian format.
	/// </summary>
	/// <returns>Float value.</returns>
	public float ReadFloat()
	{
		var bytes = _reader.ReadBytes(4);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return BitConverter.ToSingle(bytes, 0);
	}

	/// <summary>
	/// Reads a double in big-endian format.
	/// </summary>
	/// <returns>Double value.</returns>
	public double ReadDouble()
	{
		var bytes = _reader.ReadBytes(8);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return BitConverter.ToDouble(bytes, 0);
	}

	/// <summary>
	/// Reads a UTF-8 string in Java/SFS2X format (length prefix + bytes).
	/// </summary>
	/// <returns>String value.</returns>
	public string ReadUTF()
	{
		var length = ReadShort();
		if (length < 0)
		{
			throw new InvalidDataException($"Invalid UTF string length: {length}");
		}

		if (length == 0)
		{
			return string.Empty;
		}

		var bytes = _reader.ReadBytes(length);
		return Encoding.UTF8.GetString(bytes);
	}

	/// <summary>
	/// Reads the specified number of bytes.
	/// </summary>
	/// <param name="count">Number of bytes to read.</param>
	/// <returns>Byte array.</returns>
	public byte[] ReadBytes(int count)
	{
		return _reader.ReadBytes(count);
	}

	/// <summary>
	/// Gets the current position in the stream.
	/// </summary>
	public long Position
	{
		get => _stream.Position;
		set => _stream.Position = value;
	}

	/// <summary>
	/// Gets the length of the stream.
	/// </summary>
	public long Length => _stream.Length;

	/// <summary>
	/// Gets the number of bytes remaining to be read.
	/// </summary>
	public long BytesRemaining => Length - Position;

	/// <summary>
	/// Checks if there are more bytes to read.
	/// </summary>
	public bool HasBytesRemaining => BytesRemaining > 0;

	/// <summary>
	/// Disposes of the reader and underlying stream.
	/// </summary>
	public void Dispose()
	{
		if (!_disposed)
		{
			_reader?.Dispose();
			_stream?.Dispose();
			_disposed = true;
		}
		GC.SuppressFinalize(this);
	}
}