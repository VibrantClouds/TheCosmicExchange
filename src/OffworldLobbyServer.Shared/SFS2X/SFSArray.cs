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
	/// Serializes the SFSArray to binary format.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	public void Serialize(BinaryWriter writer)
	{
		writer.Write(_elements.Count);

		foreach (var element in _elements)
		{
			SerializeElement(writer, element);
		}
	}

	/// <summary>
	/// Deserializes an SFSArray from binary format.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized SFSArray.</returns>
	public static SFSArray Deserialize(BinaryReader reader)
	{
		var array = new SFSArray();
		var count = reader.ReadInt32();

		for (int i = 0; i < count; i++)
		{
			var element = DeserializeElement(reader);
			array._elements.Add(element);
		}

		return array;
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

	/// <summary>
	/// Serializes a single element.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="element">Element to serialize.</param>
	private static void SerializeElement(BinaryWriter writer, object? element)
	{
		switch (element)
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
				throw new NotSupportedException($"Unsupported element type: {element.GetType()}");
		}
	}

	/// <summary>
	/// Deserializes a single element.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized element.</returns>
	private static object? DeserializeElement(BinaryReader reader)
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
			7 => SFSObject.Deserialize(reader),
			_ => throw new NotSupportedException($"Unsupported element type indicator: {type}")
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
