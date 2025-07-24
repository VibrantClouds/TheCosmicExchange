namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// Type-safe wrapper for SFS2X data values.
/// Stores the data type and value together for proper serialization.
/// </summary>
public class SFS2XDataWrapper
{
	/// <summary>
	/// The SFS2X data type.
	/// </summary>
	public SFS2XDataType Type { get; }

	/// <summary>
	/// The actual data value.
	/// </summary>
	public object? Data { get; }

	/// <summary>
	/// Initializes a new instance of the SFS2XDataWrapper class.
	/// </summary>
	/// <param name="type">The SFS2X data type.</param>
	/// <param name="data">The data value.</param>
	public SFS2XDataWrapper(SFS2XDataType type, object? data)
	{
		Type = type;
		Data = data;
	}

	/// <summary>
	/// Gets the data as a boolean value.
	/// </summary>
	/// <returns>Boolean value.</returns>
	/// <exception cref="InvalidCastException">Data is not a boolean.</exception>
	public bool GetBool()
	{
		if (Type != SFS2XDataType.BOOL)
		{
			throw new InvalidCastException($"Data type is {Type}, not BOOL");
		}

		return Data as bool? ?? false;
	}

	/// <summary>
	/// Gets the data as a byte value.
	/// </summary>
	/// <returns>Byte value.</returns>
	/// <exception cref="InvalidCastException">Data is not a byte.</exception>
	public byte GetByte()
	{
		if (Type != SFS2XDataType.BYTE)
		{
			throw new InvalidCastException($"Data type is {Type}, not BYTE");
		}

		return Data as byte? ?? 0;
	}

	/// <summary>
	/// Gets the data as a short value.
	/// </summary>
	/// <returns>Short value.</returns>
	/// <exception cref="InvalidCastException">Data is not a short.</exception>
	public short GetShort()
	{
		if (Type != SFS2XDataType.SHORT)
		{
			throw new InvalidCastException($"Data type is {Type}, not SHORT");
		}

		return Data as short? ?? 0;
	}

	/// <summary>
	/// Gets the data as an integer value.
	/// </summary>
	/// <returns>Integer value.</returns>
	/// <exception cref="InvalidCastException">Data is not an integer.</exception>
	public int GetInt()
	{
		if (Type != SFS2XDataType.INT)
		{
			throw new InvalidCastException($"Data type is {Type}, not INT");
		}

		return Data as int? ?? 0;
	}

	/// <summary>
	/// Gets the data as a long value.
	/// </summary>
	/// <returns>Long value.</returns>
	/// <exception cref="InvalidCastException">Data is not a long.</exception>
	public long GetLong()
	{
		if (Type != SFS2XDataType.LONG)
		{
			throw new InvalidCastException($"Data type is {Type}, not LONG");
		}

		return Data as long? ?? 0;
	}

	/// <summary>
	/// Gets the data as a float value.
	/// </summary>
	/// <returns>Float value.</returns>
	/// <exception cref="InvalidCastException">Data is not a float.</exception>
	public float GetFloat()
	{
		if (Type != SFS2XDataType.FLOAT)
		{
			throw new InvalidCastException($"Data type is {Type}, not FLOAT");
		}

		return Data as float? ?? 0f;
	}

	/// <summary>
	/// Gets the data as a double value.
	/// </summary>
	/// <returns>Double value.</returns>
	/// <exception cref="InvalidCastException">Data is not a double.</exception>
	public double GetDouble()
	{
		if (Type != SFS2XDataType.DOUBLE)
		{
			throw new InvalidCastException($"Data type is {Type}, not DOUBLE");
		}

		return Data as double? ?? 0.0;
	}

	/// <summary>
	/// Gets the data as a UTF-8 string value.
	/// </summary>
	/// <returns>String value.</returns>
	/// <exception cref="InvalidCastException">Data is not a string.</exception>
	public string GetUtfString()
	{
		if (Type != SFS2XDataType.UTF_STRING && Type != SFS2XDataType.TEXT)
		{
			throw new InvalidCastException($"Data type is {Type}, not UTF_STRING or TEXT");
		}

		return Data as string ?? string.Empty;
	}

	/// <summary>
	/// Gets the data as an SFSObject.
	/// </summary>
	/// <returns>SFSObject value.</returns>
	/// <exception cref="InvalidCastException">Data is not an SFSObject.</exception>
	public SFSObject GetSFSObject()
	{
		if (Type != SFS2XDataType.SFS_OBJECT)
		{
			throw new InvalidCastException($"Data type is {Type}, not SFS_OBJECT");
		}

		return Data as SFSObject ?? new SFSObject();
	}

	/// <summary>
	/// Gets the data as an SFSArray.
	/// </summary>
	/// <returns>SFSArray value.</returns>
	/// <exception cref="InvalidCastException">Data is not an SFSArray.</exception>
	public SFSArray GetSFSArray()
	{
		if (Type != SFS2XDataType.SFS_ARRAY)
		{
			throw new InvalidCastException($"Data type is {Type}, not SFS_ARRAY");
		}

		return Data as SFSArray ?? new SFSArray();
	}

	/// <summary>
	/// Gets the data as a boolean array.
	/// </summary>
	/// <returns>Boolean array.</returns>
	/// <exception cref="InvalidCastException">Data is not a boolean array.</exception>
	public bool[] GetBoolArray()
	{
		if (Type != SFS2XDataType.BOOL_ARRAY)
		{
			throw new InvalidCastException($"Data type is {Type}, not BOOL_ARRAY");
		}

		return Data as bool[] ?? Array.Empty<bool>();
	}

	/// <summary>
	/// Gets the data as a byte array.
	/// </summary>
	/// <returns>Byte array.</returns>
	/// <exception cref="InvalidCastException">Data is not a byte array.</exception>
	public byte[] GetByteArray()
	{
		if (Type != SFS2XDataType.BYTE_ARRAY)
		{
			throw new InvalidCastException($"Data type is {Type}, not BYTE_ARRAY");
		}

		return Data as byte[] ?? Array.Empty<byte>();
	}

	/// <summary>
	/// Gets the data as a string array.
	/// </summary>
	/// <returns>String array.</returns>
	/// <exception cref="InvalidCastException">Data is not a string array.</exception>
	public string[] GetUtfStringArray()
	{
		if (Type != SFS2XDataType.UTF_STRING_ARRAY)
		{
			throw new InvalidCastException($"Data type is {Type}, not UTF_STRING_ARRAY");
		}

		return Data as string[] ?? Array.Empty<string>();
	}
}