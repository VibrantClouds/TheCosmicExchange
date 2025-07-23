namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// SFS2X Object implementation compatible with the game's ISFSObject
/// Used for PlayerID -> value mappings in team and handicap assignments.
/// </summary>
public class SFSObject
{
	private readonly Dictionary<string, object?> _data = new ();

	/// <summary>
	/// Gets all keys in the object.
	/// </summary>
	/// <returns>Array of keys.</returns>
	public string[] GetKeys()
	{
		return _data.Keys.ToArray();
	}

	/// <summary>
	/// Checks if a key exists.
	/// </summary>
	/// <param name="key">Key to check.</param>
	/// <returns>True if key exists.</returns>
	public bool ContainsKey(string key)
	{
		return _data.ContainsKey(key);
	}

	/// <summary>
	/// Gets the number of key-value pairs.
	/// </summary>
	public int Size => _data.Count;

	/// <summary>
	/// Sets a UTF-8 string value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">String value.</param>
	public void PutUtfString(string key, string value)
	{
		_data[key] = value ?? string.Empty;
	}

	/// <summary>
	/// Sets a byte value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">Byte value.</param>
	public void PutByte(string key, byte value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// Sets a short value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">Short value.</param>
	public void PutShort(string key, short value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// Sets an integer value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">Integer value.</param>
	public void PutInt(string key, int value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// Sets a long value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">Long value.</param>
	public void PutLong(string key, long value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// Sets a boolean value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">Boolean value.</param>
	public void PutBool(string key, bool value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// Sets a boolean array value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">Boolean array.</param>
	public void PutBoolArray(string key, bool[] value)
	{
		_data[key] = value ?? Array.Empty<bool>();
	}

	/// <summary>
	/// Sets an SFSObject value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="value">SFSObject value.</param>
	public void PutSFSObject(string key, SFSObject value)
	{
		_data[key] = value ?? new SFSObject();
	}

	/// <summary>
	/// Gets a UTF-8 string value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>String value or empty string if not found.</returns>
	public string GetUtfString(string key)
	{
		return _data.TryGetValue(key, out var value) ? value as string ?? string.Empty : string.Empty;
	}

	/// <summary>
	/// Gets a byte value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>Byte value or 0 if not found.</returns>
	/// <exception cref="InvalidCastException">Value is not a byte.</exception>
	public byte GetByte(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return 0;
		}

		if (value is byte byteValue)
		{
			return byteValue;
		}

		throw new InvalidCastException($"Value for key '{key}' is not a byte");
	}

	/// <summary>
	/// Gets a short value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>Short value or 0 if not found.</returns>
	/// <exception cref="InvalidCastException">Value is not a short.</exception>
	public short GetShort(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return 0;
		}

		if (value is short shortValue)
		{
			return shortValue;
		}

		throw new InvalidCastException($"Value for key '{key}' is not a short");
	}

	/// <summary>
	/// Gets an integer value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>Integer value or 0 if not found.</returns>
	/// <exception cref="InvalidCastException">Value is not an integer.</exception>
	public int GetInt(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return 0;
		}

		if (value is int intValue)
		{
			return intValue;
		}

		throw new InvalidCastException($"Value for key '{key}' is not an int");
	}

	/// <summary>
	/// Gets a long value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>Long value or 0 if not found.</returns>
	/// <exception cref="InvalidCastException">Value is not a long.</exception>
	public long GetLong(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return 0;
		}

		if (value is long longValue)
		{
			return longValue;
		}

		throw new InvalidCastException($"Value for key '{key}' is not a long");
	}

	/// <summary>
	/// Gets a boolean value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>Boolean value or false if not found.</returns>
	/// <exception cref="InvalidCastException">Value is not a boolean.</exception>
	public bool GetBool(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return false;
		}

		if (value is bool boolValue)
		{
			return boolValue;
		}

		throw new InvalidCastException($"Value for key '{key}' is not a bool");
	}

	/// <summary>
	/// Gets a boolean array value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>Boolean array or empty array if not found.</returns>
	/// <exception cref="InvalidCastException">Value is not a boolean array.</exception>
	public bool[] GetBoolArray(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return Array.Empty<bool>();
		}

		if (value is bool[] boolArray)
		{
			return boolArray;
		}

		throw new InvalidCastException($"Value for key '{key}' is not a bool array");
	}

	/// <summary>
	/// Gets an SFSObject value.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>SFSObject or new empty object if not found.</returns>
	/// <exception cref="InvalidCastException">Value is not an SFSObject.</exception>
	public SFSObject GetSFSObject(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return new SFSObject();
		}

		if (value is SFSObject sfsObject)
		{
			return sfsObject;
		}

		throw new InvalidCastException($"Value for key '{key}' is not an SFSObject");
	}

	/// <summary>
	/// Removes a key-value pair.
	/// </summary>
	/// <param name="key">Key to remove.</param>
	/// <returns>True if key existed and was removed.</returns>
	public bool Remove(string key)
	{
		return _data.Remove(key);
	}

	/// <summary>
	/// Clears all key-value pairs.
	/// </summary>
	public void Clear()
	{
		_data.Clear();
	}

	/// <summary>
	/// Gets a data wrapper for the specified key.
	/// Required by SFS2XSerializer for proper type handling.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <returns>SFS2XDataWrapper for the value.</returns>
	internal SFS2XDataWrapper GetData(string key)
	{
		if (!_data.TryGetValue(key, out var value))
		{
			return new SFS2XDataWrapper(SFS2XDataType.NULL, null);
		}

		return value switch
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
			_ => throw new NotSupportedException($"Unsupported value type: {value.GetType()}")
		};
	}

	/// <summary>
	/// Puts a data wrapper value into the object.
	/// Required by SFS2XSerializer for proper type handling.
	/// </summary>
	/// <param name="key">Key identifier.</param>
	/// <param name="wrapper">Data wrapper to store.</param>
	internal void Put(string key, SFS2XDataWrapper wrapper)
	{
		_data[key] = wrapper.Data;
	}

	/// <summary>
	/// Serializes the SFSObject to SFS2X binary format.
	/// Uses the official SFS2X serialization protocol.
	/// </summary>
	/// <returns>Binary representation.</returns>
	public byte[] Serialize()
	{
		return SFS2XSerializer.SerializeObject(this);
	}

	/// <summary>
	/// Deserializes an SFSObject from SFS2X binary format.
	/// Uses the official SFS2X deserialization protocol.
	/// </summary>
	/// <param name="data">Binary data.</param>
	/// <returns>Deserialized SFSObject.</returns>
	public static SFSObject Deserialize(byte[] data)
	{
		return SFS2XSerializer.DeserializeObject(data);
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
	/// <returns>Deserialized SFSObject.</returns>
	[Obsolete("Use Deserialize(byte[]) method instead for SFS2X compatibility")]
	public static SFSObject Deserialize(BinaryReader reader)
	{
		// Read the entire remaining stream as bytes and use SFS2X deserializer
		var stream = reader.BaseStream;
		var remaining = (int)(stream.Length - stream.Position);
		var data = reader.ReadBytes(remaining);
		return Deserialize(data);
	}
}
