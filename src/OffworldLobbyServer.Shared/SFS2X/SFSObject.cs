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
	/// Serializes the SFSObject to binary format.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	public void Serialize(BinaryWriter writer)
	{
		writer.Write(_data.Count);

		foreach (var kvp in _data)
		{
			writer.Write(kvp.Key);
			SerializeValue(writer, kvp.Value);
		}
	}

	/// <summary>
	/// Deserializes an SFSObject from binary format.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized SFSObject.</returns>
	public static SFSObject Deserialize(BinaryReader reader)
	{
		var obj = new SFSObject();
		var count = reader.ReadInt32();

		for (int i = 0; i < count; i++)
		{
			var key = reader.ReadString();
			var value = DeserializeValue(reader);
			obj._data[key] = value;
		}

		return obj;
	}

	/// <summary>
	/// Serializes a single value.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	private static void SerializeValue(BinaryWriter writer, object? value)
	{
		switch (value)
		{
			case null:
				writer.Write((byte)0); // Null indicator
				break;
			case string str:
				writer.Write((byte)1);
				writer.Write(str);
				break;
			case byte b:
				writer.Write((byte)2);
				writer.Write(b);
				break;
			case short s:
				writer.Write((byte)3);
				writer.Write(s);
				break;
			case int i:
				writer.Write((byte)4);
				writer.Write(i);
				break;
			case long l:
				writer.Write((byte)8);
				writer.Write(l);
				break;
			case bool bl:
				writer.Write((byte)5);
				writer.Write(bl);
				break;
			case bool[] bArr:
				writer.Write((byte)6);
				writer.Write(bArr.Length);
				foreach (var b in bArr)
				{
					writer.Write(b);
				}

				break;
			case SFSObject obj:
				writer.Write((byte)7);
				obj.Serialize(writer);
				break;
			default:
				throw new NotSupportedException($"Unsupported value type: {value.GetType()}");
		}
	}

	/// <summary>
	/// Deserializes a single value.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized value.</returns>
	private static object? DeserializeValue(BinaryReader reader)
	{
		var type = reader.ReadByte();

		return type switch
		{
			0 => null,
			1 => reader.ReadString(),
			2 => reader.ReadByte(),
			3 => reader.ReadInt16(),
			4 => reader.ReadInt32(),
			5 => reader.ReadBoolean(),
			6 => DeserializeBoolArray(reader),
			7 => Deserialize(reader),
			8 => reader.ReadInt64(),
			_ => throw new NotSupportedException($"Unsupported value type indicator: {type}")
		};
	}

	/// <summary>
	/// Deserializes a boolean array.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Boolean array.</returns>
	private static bool[] DeserializeBoolArray(BinaryReader reader)
	{
		var length = reader.ReadInt32();
		var array = new bool[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = reader.ReadBoolean();
		}

		return array;
	}
}
