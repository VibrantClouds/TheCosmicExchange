namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// SFS2X Array implementation compatible with the game's ISFSArray
/// Supports mixed-type indexed storage as used in lobby settings serialization.
/// </summary>
public class SFSArray
{
	private readonly List<object?> _elements = new ();

	/// <summary>
	/// Gets the number of elements in the array.
	/// </summary>
	public int Size => _elements.Count;

	/// <summary>
	/// Adds a UTF-8 string to the array.
	/// </summary>
	/// <param name="value">String value to add.</param>
	public void AddUtfString(string value)
	{
		_elements.Add(value ?? string.Empty);
	}

	/// <summary>
	/// Adds a byte to the array.
	/// </summary>
	/// <param name="value">Byte value to add.</param>
	public void AddByte(byte value)
	{
		_elements.Add(value);
	}

	/// <summary>
	/// Adds a short to the array.
	/// </summary>
	/// <param name="value">Short value to add.</param>
	public void AddShort(short value)
	{
		_elements.Add(value);
	}

	/// <summary>
	/// Adds an integer to the array.
	/// </summary>
	/// <param name="value">Integer value to add.</param>
	public void AddInt(int value)
	{
		_elements.Add(value);
	}

	/// <summary>
	/// Adds a boolean to the array.
	/// </summary>
	/// <param name="value">Boolean value to add.</param>
	public void AddBool(bool value)
	{
		_elements.Add(value);
	}

	/// <summary>
	/// Adds a boolean array to the array.
	/// </summary>
	/// <param name="value">Boolean array to add.</param>
	public void AddBoolArray(bool[] value)
	{
		_elements.Add(value ?? Array.Empty<bool>());
	}

	/// <summary>
	/// Adds an SFSObject to the array.
	/// </summary>
	/// <param name="value">SFSObject to add.</param>
	public void AddSFSObject(SFSObject value)
	{
		_elements.Add(value ?? new SFSObject());
	}

	/// <summary>
	/// Gets a UTF-8 string at the specified index.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>String value.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	/// <exception cref="InvalidCastException">Element is not a string.</exception>
	public string GetUtfString(int index)
	{
		ValidateIndex(index);
		return _elements[index] as string ?? string.Empty;
	}

	/// <summary>
	/// Gets a byte at the specified index.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>Byte value.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	/// <exception cref="InvalidCastException">Element is not a byte.</exception>
	public byte GetByte(int index)
	{
		ValidateIndex(index);
		if (_elements[index] is byte byteValue)
		{
			return byteValue;
		}

		throw new InvalidCastException($"Element at index {index} is not a byte");
	}

	/// <summary>
	/// Gets a short at the specified index.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>Short value.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	/// <exception cref="InvalidCastException">Element is not a short.</exception>
	public short GetShort(int index)
	{
		ValidateIndex(index);
		if (_elements[index] is short shortValue)
		{
			return shortValue;
		}

		throw new InvalidCastException($"Element at index {index} is not a short");
	}

	/// <summary>
	/// Gets an integer at the specified index.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>Integer value.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	/// <exception cref="InvalidCastException">Element is not an integer.</exception>
	public int GetInt(int index)
	{
		ValidateIndex(index);
		if (_elements[index] is int intValue)
		{
			return intValue;
		}

		throw new InvalidCastException($"Element at index {index} is not an int");
	}

	/// <summary>
	/// Gets a boolean at the specified index.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>Boolean value.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	/// <exception cref="InvalidCastException">Element is not a boolean.</exception>
	public bool GetBool(int index)
	{
		ValidateIndex(index);
		if (_elements[index] is bool boolValue)
		{
			return boolValue;
		}

		throw new InvalidCastException($"Element at index {index} is not a bool");
	}

	/// <summary>
	/// Gets a boolean array at the specified index.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>Boolean array.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	/// <exception cref="InvalidCastException">Element is not a boolean array.</exception>
	public bool[] GetBoolArray(int index)
	{
		ValidateIndex(index);
		if (_elements[index] is bool[] boolArray)
		{
			return boolArray;
		}

		throw new InvalidCastException($"Element at index {index} is not a bool array");
	}

	/// <summary>
	/// Gets an SFSObject at the specified index.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>SFSObject.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	/// <exception cref="InvalidCastException">Element is not an SFSObject.</exception>
	public SFSObject GetSFSObject(int index)
	{
		ValidateIndex(index);
		if (_elements[index] is SFSObject sfsObject)
		{
			return sfsObject;
		}

		throw new InvalidCastException($"Element at index {index} is not an SFSObject");
	}

	/// <summary>
	/// Gets a data wrapper for the element at the specified index.
	/// Required by SFS2XSerializer for proper type handling.
	/// </summary>
	/// <param name="index">Array index.</param>
	/// <returns>SFS2XDataWrapper for the element.</returns>
	internal SFS2XDataWrapper GetWrappedElementAt(int index)
	{
		ValidateIndex(index);
		var element = _elements[index];

		return element switch
		{
			null => new SFS2XDataWrapper(SFS2XDataType.NULL, null),
			bool boolValue => new SFS2XDataWrapper(SFS2XDataType.BOOL, boolValue),
			byte byteValue => new SFS2XDataWrapper(SFS2XDataType.BYTE, byteValue),
			short shortValue => new SFS2XDataWrapper(SFS2XDataType.SHORT, shortValue),
			int intValue => new SFS2XDataWrapper(SFS2XDataType.INT, intValue),
			long longValue => new SFS2XDataWrapper(SFS2XDataType.LONG, longValue),
			float floatValue => new SFS2XDataWrapper(SFS2XDataType.FLOAT, floatValue),
			double doubleValue => new SFS2XDataWrapper(SFS2XDataType.DOUBLE, doubleValue),
			string stringValue => new SFS2XDataWrapper(SFS2XDataType.UTF_STRING, stringValue),
			bool[] boolArray => new SFS2XDataWrapper(SFS2XDataType.BOOL_ARRAY, boolArray),
			byte[] byteArray => new SFS2XDataWrapper(SFS2XDataType.BYTE_ARRAY, byteArray),
			string[] stringArray => new SFS2XDataWrapper(SFS2XDataType.UTF_STRING_ARRAY, stringArray),
			SFSObject sfsObject => new SFS2XDataWrapper(SFS2XDataType.SFS_OBJECT, sfsObject),
			SFSArray sfsArray => new SFS2XDataWrapper(SFS2XDataType.SFS_ARRAY, sfsArray),
			_ => throw new NotSupportedException($"Unsupported element type: {element.GetType()}")
		};
	}

	/// <summary>
	/// Adds a data wrapper to the array.
	/// Required by SFS2XSerializer for proper type handling.
	/// </summary>
	/// <param name="wrapper">Data wrapper to add.</param>
	internal void Add(SFS2XDataWrapper wrapper)
	{
		_elements.Add(wrapper.Data);
	}

	/// <summary>
	/// Serializes the SFSArray to SFS2X binary format.
	/// Uses the official SFS2X serialization protocol.
	/// </summary>
	/// <returns>Binary representation.</returns>
	public byte[] Serialize()
	{
		return SFS2XSerializer.SerializeArray(this);
	}

	/// <summary>
	/// Deserializes an SFSArray from SFS2X binary format.
	/// Uses the official SFS2X deserialization protocol.
	/// </summary>
	/// <param name="data">Binary data.</param>
	/// <returns>Deserialized SFSArray.</returns>
	public static SFSArray Deserialize(byte[] data)
	{
		return SFS2XSerializer.DeserializeArray(data);
	}

	/// <summary>
	/// Legacy serialization method for backward compatibility.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	[Obsolete("Use Serialize() method instead for SFS2X compatibility")]
	public void Serialize(BinaryWriter writer)
	{
		var data = Serialize();
		writer.Write(data);
	}

	/// <summary>
	/// Legacy deserialization method for backward compatibility.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized SFSArray.</returns>
	[Obsolete("Use Deserialize(byte[]) method instead for SFS2X compatibility")]
	public static SFSArray Deserialize(BinaryReader reader)
	{
		// Read the entire remaining stream as bytes and use SFS2X deserializer
		var stream = reader.BaseStream;
		var remaining = (int)(stream.Length - stream.Position);
		var data = reader.ReadBytes(remaining);
		return Deserialize(data);
	}

	/// <summary>
	/// Validates array index.
	/// </summary>
	/// <param name="index">Index to validate.</param>
	/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
	private void ValidateIndex(int index)
	{
		if (index < 0 || index >= _elements.Count)
		{
			throw new ArgumentOutOfRangeException(
				nameof(index),
				$"Index {index} is out of range. Array size: {_elements.Count}");
		}
	}

}
