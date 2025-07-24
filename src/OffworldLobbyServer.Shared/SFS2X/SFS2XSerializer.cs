namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// SFS2X binary protocol serializer.
/// Implements the exact binary format used by SmartFoxServer 2X clients.
/// Based on the DefaultSFSDataSerializer from the official library.
/// </summary>
public static class SFS2XSerializer
{
	/// <summary>
	/// Serializes an SFSObject to binary format.
	/// Format: [0x12][count:short][key:UTF8][type:byte][value:typed_data]...
	/// </summary>
	/// <param name="obj">SFSObject to serialize.</param>
	/// <returns>Binary representation.</returns>
	public static byte[] SerializeObject(SFSObject obj)
	{
		using var writer = new SFS2XBinaryWriter();
		
		// Write SFSObject header: type (18) + count
		writer.WriteByte((byte)SFS2XDataType.SFS_OBJECT);
		writer.WriteShort((short)obj.Size);
		
		// Write each key-value pair
		foreach (var key in obj.GetKeys())
		{
			var wrapper = obj.GetData(key);
			
			// Write key as UTF-8 string
			writer.WriteUTF(key);
			
			// Write value with type prefix
			SerializeValue(writer, wrapper);
		}
		
		return writer.ToArray();
	}

	/// <summary>
	/// Serializes an SFSArray to binary format.
	/// Format: [0x11][count:short][type:byte][value:typed_data]...
	/// </summary>
	/// <param name="array">SFSArray to serialize.</param>
	/// <returns>Binary representation.</returns>
	public static byte[] SerializeArray(SFSArray array)
	{
		using var writer = new SFS2XBinaryWriter();
		
		// Write SFSArray header: type (17) + count
		writer.WriteByte((byte)SFS2XDataType.SFS_ARRAY);
		writer.WriteShort((short)array.Size);
		
		// Write each element
		for (int i = 0; i < array.Size; i++)
		{
			var wrapper = array.GetWrappedElementAt(i);
			SerializeValue(writer, wrapper);
		}
		
		return writer.ToArray();
	}

	/// <summary>
	/// Deserializes an SFSObject from binary format.
	/// </summary>
	/// <param name="data">Binary data.</param>
	/// <returns>Deserialized SFSObject.</returns>
	public static SFSObject DeserializeObject(byte[] data)
	{
		using var reader = new SFS2XBinaryReader(data);
		return DeserializeObject(reader);
	}

	/// <summary>
	/// Deserializes an SFSObject from a binary reader.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized SFSObject.</returns>
	public static SFSObject DeserializeObject(SFS2XBinaryReader reader)
	{
		var obj = new SFSObject();
		
		// Read and validate type
		var type = (SFS2XDataType)reader.ReadByte();
		if (type != SFS2XDataType.SFS_OBJECT)
		{
			throw new InvalidDataException($"Invalid SFS2X data type. Expected: SFS_OBJECT, found: {type}");
		}
		
		// Read element count
		var count = reader.ReadShort();
		if (count < 0)
		{
			throw new InvalidDataException($"Invalid SFS2X object size: {count}");
		}
		
		// Read each key-value pair
		for (int i = 0; i < count; i++)
		{
			var key = reader.ReadUTF();
			var wrapper = DeserializeValue(reader);
			obj.Put(key, wrapper);
		}
		
		return obj;
	}

	/// <summary>
	/// Deserializes an SFSArray from binary format.
	/// </summary>
	/// <param name="data">Binary data.</param>
	/// <returns>Deserialized SFSArray.</returns>
	public static SFSArray DeserializeArray(byte[] data)
	{
		using var reader = new SFS2XBinaryReader(data);
		return DeserializeArray(reader);
	}

	/// <summary>
	/// Deserializes an SFSArray from a binary reader.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized SFSArray.</returns>
	public static SFSArray DeserializeArray(SFS2XBinaryReader reader)
	{
		var array = new SFSArray();
		
		// Read and validate type
		var type = (SFS2XDataType)reader.ReadByte();
		if (type != SFS2XDataType.SFS_ARRAY)
		{
			throw new InvalidDataException($"Invalid SFS2X data type. Expected: SFS_ARRAY, found: {type}");
		}
		
		// Read element count
		var count = reader.ReadShort();
		if (count < 0)
		{
			throw new InvalidDataException($"Invalid SFS2X array size: {count}");
		}
		
		// Read each element
		for (int i = 0; i < count; i++)
		{
			var wrapper = DeserializeValue(reader);
			array.Add(wrapper);
		}
		
		return array;
	}

	/// <summary>
	/// Serializes a single value with its type prefix.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="wrapper">Data wrapper to serialize.</param>
	private static void SerializeValue(SFS2XBinaryWriter writer, SFS2XDataWrapper wrapper)
	{
		// Write type byte
		writer.WriteByte((byte)wrapper.Type);
		
		// Write value based on type
		switch (wrapper.Type)
		{
			case SFS2XDataType.NULL:
				// No data for null
				break;
				
			case SFS2XDataType.BOOL:
				writer.WriteBool(wrapper.GetBool());
				break;
				
			case SFS2XDataType.BYTE:
				writer.WriteByte(wrapper.GetByte());
				break;
				
			case SFS2XDataType.SHORT:
				writer.WriteShort(wrapper.GetShort());
				break;
				
			case SFS2XDataType.INT:
				writer.WriteInt(wrapper.GetInt());
				break;
				
			case SFS2XDataType.LONG:
				writer.WriteLong(wrapper.GetLong());
				break;
				
			case SFS2XDataType.FLOAT:
				writer.WriteFloat(wrapper.GetFloat());
				break;
				
			case SFS2XDataType.DOUBLE:
				writer.WriteDouble(wrapper.GetDouble());
				break;
				
			case SFS2XDataType.UTF_STRING:
			case SFS2XDataType.TEXT:
				writer.WriteUTF(wrapper.GetUtfString());
				break;
				
			case SFS2XDataType.BOOL_ARRAY:
				SerializeBoolArray(writer, wrapper.GetBoolArray());
				break;
				
			case SFS2XDataType.BYTE_ARRAY:
				SerializeByteArray(writer, wrapper.GetByteArray());
				break;
				
			case SFS2XDataType.UTF_STRING_ARRAY:
				SerializeStringArray(writer, wrapper.GetUtfStringArray());
				break;
				
			case SFS2XDataType.SFS_OBJECT:
				var objData = SerializeObject(wrapper.GetSFSObject());
				writer.WriteBytes(objData[1..]); // Skip type byte, already written
				break;
				
			case SFS2XDataType.SFS_ARRAY:
				var arrData = SerializeArray(wrapper.GetSFSArray());
				writer.WriteBytes(arrData[1..]); // Skip type byte, already written
				break;
				
			default:
				throw new NotSupportedException($"Unsupported SFS2X data type: {wrapper.Type}");
		}
	}

	/// <summary>
	/// Deserializes a single value based on its type.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <returns>Deserialized data wrapper.</returns>
	private static SFS2XDataWrapper DeserializeValue(SFS2XBinaryReader reader)
	{
		var type = (SFS2XDataType)reader.ReadByte();
		
		return type switch
		{
			SFS2XDataType.NULL => new SFS2XDataWrapper(SFS2XDataType.NULL, null),
			SFS2XDataType.BOOL => new SFS2XDataWrapper(SFS2XDataType.BOOL, reader.ReadBool()),
			SFS2XDataType.BYTE => new SFS2XDataWrapper(SFS2XDataType.BYTE, reader.ReadByte()),
			SFS2XDataType.SHORT => new SFS2XDataWrapper(SFS2XDataType.SHORT, reader.ReadShort()),
			SFS2XDataType.INT => new SFS2XDataWrapper(SFS2XDataType.INT, reader.ReadInt()),
			SFS2XDataType.LONG => new SFS2XDataWrapper(SFS2XDataType.LONG, reader.ReadLong()),
			SFS2XDataType.FLOAT => new SFS2XDataWrapper(SFS2XDataType.FLOAT, reader.ReadFloat()),
			SFS2XDataType.DOUBLE => new SFS2XDataWrapper(SFS2XDataType.DOUBLE, reader.ReadDouble()),
			SFS2XDataType.UTF_STRING => new SFS2XDataWrapper(SFS2XDataType.UTF_STRING, reader.ReadUTF()),
			SFS2XDataType.TEXT => new SFS2XDataWrapper(SFS2XDataType.TEXT, reader.ReadUTF()),
			SFS2XDataType.BOOL_ARRAY => new SFS2XDataWrapper(SFS2XDataType.BOOL_ARRAY, DeserializeBoolArray(reader)),
			SFS2XDataType.BYTE_ARRAY => new SFS2XDataWrapper(SFS2XDataType.BYTE_ARRAY, DeserializeByteArray(reader)),
			SFS2XDataType.UTF_STRING_ARRAY => new SFS2XDataWrapper(SFS2XDataType.UTF_STRING_ARRAY, DeserializeStringArray(reader)),
			SFS2XDataType.SFS_OBJECT => new SFS2XDataWrapper(SFS2XDataType.SFS_OBJECT, DeserializeNestedObject(reader)),
			SFS2XDataType.SFS_ARRAY => new SFS2XDataWrapper(SFS2XDataType.SFS_ARRAY, DeserializeNestedArray(reader)),
			_ => throw new NotSupportedException($"Unsupported SFS2X data type: {type}")
		};
	}

	#region Array Serialization

	private static void SerializeBoolArray(SFS2XBinaryWriter writer, bool[] array)
	{
		writer.WriteShort((short)array.Length);
		foreach (var value in array)
		{
			writer.WriteBool(value);
		}
	}

	private static void SerializeByteArray(SFS2XBinaryWriter writer, byte[] array)
	{
		writer.WriteInt(array.Length);
		writer.WriteBytes(array);
	}

	private static void SerializeStringArray(SFS2XBinaryWriter writer, string[] array)
	{
		writer.WriteShort((short)array.Length);
		foreach (var value in array)
		{
			writer.WriteUTF(value);
		}
	}

	#endregion

	#region Array Deserialization

	private static bool[] DeserializeBoolArray(SFS2XBinaryReader reader)
	{
		var length = reader.ReadShort();
		if (length < 0)
		{
			throw new InvalidDataException($"Invalid bool array length: {length}");
		}

		var array = new bool[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = reader.ReadBool();
		}
		return array;
	}

	private static byte[] DeserializeByteArray(SFS2XBinaryReader reader)
	{
		var length = reader.ReadInt();
		if (length < 0)
		{
			throw new InvalidDataException($"Invalid byte array length: {length}");
		}

		return reader.ReadBytes(length);
	}

	private static string[] DeserializeStringArray(SFS2XBinaryReader reader)
	{
		var length = reader.ReadShort();
		if (length < 0)
		{
			throw new InvalidDataException($"Invalid string array length: {length}");
		}

		var array = new string[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = reader.ReadUTF();
		}
		return array;
	}

	private static SFSObject DeserializeNestedObject(SFS2XBinaryReader reader)
	{
		// Need to rewind by one byte to read the type header
		reader.Position--;
		return DeserializeObject(reader);
	}

	private static SFSArray DeserializeNestedArray(SFS2XBinaryReader reader)
	{
		// Need to rewind by one byte to read the type header
		reader.Position--;
		return DeserializeArray(reader);
	}

	#endregion
}