using System.Text;

namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// Binary writer for SFS2X protocol data.
/// Implements the exact binary format expected by SmartFoxServer 2X clients.
/// </summary>
public class SFS2XBinaryWriter : IDisposable
{
	private readonly MemoryStream _stream;
	private readonly BinaryWriter _writer;
	private bool _disposed = false;

	/// <summary>
	/// Initializes a new instance of the SFS2XBinaryWriter class.
	/// </summary>
	public SFS2XBinaryWriter()
	{
		_stream = new MemoryStream();
		_writer = new BinaryWriter(_stream, Encoding.UTF8);
	}

	/// <summary>
	/// Writes a byte value.
	/// </summary>
	/// <param name="value">Byte value to write.</param>
	public void WriteByte(byte value)
	{
		_writer.Write(value);
	}

	/// <summary>
	/// Writes a boolean value as a byte (0 or 1).
	/// </summary>
	/// <param name="value">Boolean value to write.</param>
	public void WriteBool(bool value)
	{
		_writer.Write(value ? (byte)1 : (byte)0);
	}

	/// <summary>
	/// Writes a short integer in big-endian format.
	/// </summary>
	/// <param name="value">Short value to write.</param>
	public void WriteShort(short value)
	{
		var bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		_writer.Write(bytes);
	}

	/// <summary>
	/// Writes an integer in big-endian format.
	/// </summary>
	/// <param name="value">Integer value to write.</param>
	public void WriteInt(int value)
	{
		var bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		_writer.Write(bytes);
	}

	/// <summary>
	/// Writes a long integer in big-endian format.
	/// </summary>
	/// <param name="value">Long value to write.</param>
	public void WriteLong(long value)
	{
		var bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		_writer.Write(bytes);
	}

	/// <summary>
	/// Writes a float in big-endian format.
	/// </summary>
	/// <param name="value">Float value to write.</param>
	public void WriteFloat(float value)
	{
		var bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		_writer.Write(bytes);
	}

	/// <summary>
	/// Writes a double in big-endian format.
	/// </summary>
	/// <param name="value">Double value to write.</param>
	public void WriteDouble(double value)
	{
		var bytes = BitConverter.GetBytes(value);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		_writer.Write(bytes);
	}

	/// <summary>
	/// Writes a UTF-8 string in Java/SFS2X format (length prefix + bytes).
	/// </summary>
	/// <param name="value">String value to write.</param>
	public void WriteUTF(string value)
	{
		value ??= string.Empty;
		var bytes = Encoding.UTF8.GetBytes(value);
		
		// Write length as big-endian short
		WriteShort((short)bytes.Length);
		
		// Write UTF-8 bytes
		_writer.Write(bytes);
	}

	/// <summary>
	/// Writes raw bytes to the stream.
	/// </summary>
	/// <param name="bytes">Byte array to write.</param>
	public void WriteBytes(byte[] bytes)
	{
		_writer.Write(bytes);
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
	/// Gets the written bytes as an array.
	/// </summary>
	/// <returns>Byte array containing all written data.</returns>
	public byte[] ToArray()
	{
		_writer.Flush();
		return _stream.ToArray();
	}

	/// <summary>
	/// Disposes of the writer and underlying stream.
	/// </summary>
	public void Dispose()
	{
		if (!_disposed)
		{
			_writer?.Dispose();
			_stream?.Dispose();
			_disposed = true;
		}
		GC.SuppressFinalize(this);
	}
}