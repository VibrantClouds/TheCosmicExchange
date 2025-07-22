namespace OffworldLobbyServer.Shared.Serialization;

/// <summary>
/// SimplifyIO-compatible serialization system
/// Implements keyed serialization with type safety matching game's SimplifyIO.Data() method
/// Format: [key_length][key_string][type_indicator][value].
/// </summary>
public static class SimplifyIOSerializer
{
	/// <summary>
	/// Type indicators for serialized data.
	/// </summary>
	private enum TypeIndicator : byte
	{
		String = 1,
		Int32 = 2,
		Int16 = 3,
		Byte = 4,
		Boolean = 5,
		Single = 6,
		BooleanArray = 7,
	}

	/// <summary>
	/// Serializes a string value with key.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	/// <param name="key">Key identifier.</param>
	public static void WriteString(BinaryWriter writer, string value, string key)
	{
		WriteKey(writer, key);
		writer.Write((byte)TypeIndicator.String);
		writer.Write(value ?? string.Empty);
	}

	/// <summary>
	/// Serializes an integer value with key.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	/// <param name="key">Key identifier.</param>
	public static void WriteInt32(BinaryWriter writer, int value, string key)
	{
		WriteKey(writer, key);
		writer.Write((byte)TypeIndicator.Int32);
		writer.Write(value);
	}

	/// <summary>
	/// Serializes a short value with key.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	/// <param name="key">Key identifier.</param>
	public static void WriteInt16(BinaryWriter writer, short value, string key)
	{
		WriteKey(writer, key);
		writer.Write((byte)TypeIndicator.Int16);
		writer.Write(value);
	}

	/// <summary>
	/// Serializes a byte value with key.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	/// <param name="key">Key identifier.</param>
	public static void WriteByte(BinaryWriter writer, byte value, string key)
	{
		WriteKey(writer, key);
		writer.Write((byte)TypeIndicator.Byte);
		writer.Write(value);
	}

	/// <summary>
	/// Serializes a boolean value with key.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	/// <param name="key">Key identifier.</param>
	public static void WriteBoolean(BinaryWriter writer, bool value, string key)
	{
		WriteKey(writer, key);
		writer.Write((byte)TypeIndicator.Boolean);
		writer.Write(value);
	}

	/// <summary>
	/// Serializes a float value with key.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	/// <param name="key">Key identifier.</param>
	public static void WriteSingle(BinaryWriter writer, float value, string key)
	{
		WriteKey(writer, key);
		writer.Write((byte)TypeIndicator.Single);
		writer.Write(value);
	}

	/// <summary>
	/// Serializes a boolean array with key.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="value">Value to serialize.</param>
	/// <param name="key">Key identifier.</param>
	public static void WriteBooleanArray(BinaryWriter writer, bool[] value, string key)
	{
		WriteKey(writer, key);
		writer.Write((byte)TypeIndicator.BooleanArray);
		writer.Write(value?.Length ?? 0);
		if (value != null)
		{
			foreach (var item in value)
			{
				writer.Write(item);
			}
		}
	}

	/// <summary>
	/// Reads a string value with key validation.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <returns>Deserialized string value.</returns>
	public static string ReadString(BinaryReader reader, string expectedKey)
	{
		ValidateKey(reader, expectedKey);
		ValidateType(reader, TypeIndicator.String);
		return reader.ReadString();
	}

	/// <summary>
	/// Reads an integer value with key validation.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <returns>Deserialized integer value.</returns>
	public static int ReadInt32(BinaryReader reader, string expectedKey)
	{
		ValidateKey(reader, expectedKey);
		ValidateType(reader, TypeIndicator.Int32);
		return reader.ReadInt32();
	}

	/// <summary>
	/// Reads a short value with key validation.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <returns>Deserialized short value.</returns>
	public static short ReadInt16(BinaryReader reader, string expectedKey)
	{
		ValidateKey(reader, expectedKey);
		ValidateType(reader, TypeIndicator.Int16);
		return reader.ReadInt16();
	}

	/// <summary>
	/// Reads a byte value with key validation.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <returns>Deserialized byte value.</returns>
	public static byte ReadByte(BinaryReader reader, string expectedKey)
	{
		ValidateKey(reader, expectedKey);
		ValidateType(reader, TypeIndicator.Byte);
		return reader.ReadByte();
	}

	/// <summary>
	/// Reads a boolean value with key validation.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <returns>Deserialized boolean value.</returns>
	public static bool ReadBoolean(BinaryReader reader, string expectedKey)
	{
		ValidateKey(reader, expectedKey);
		ValidateType(reader, TypeIndicator.Boolean);
		return reader.ReadBoolean();
	}

	/// <summary>
	/// Reads a float value with key validation.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <returns>Deserialized float value.</returns>
	public static float ReadSingle(BinaryReader reader, string expectedKey)
	{
		ValidateKey(reader, expectedKey);
		ValidateType(reader, TypeIndicator.Single);
		return reader.ReadSingle();
	}

	/// <summary>
	/// Reads a boolean array with key validation.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <returns>Deserialized boolean array.</returns>
	public static bool[] ReadBooleanArray(BinaryReader reader, string expectedKey)
	{
		ValidateKey(reader, expectedKey);
		ValidateType(reader, TypeIndicator.BooleanArray);
		var length = reader.ReadInt32();
		var array = new bool[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = reader.ReadBoolean();
		}

		return array;
	}

	/// <summary>
	/// Writes the key identifier.
	/// </summary>
	/// <param name="writer">Binary writer.</param>
	/// <param name="key">Key identifier.</param>
	private static void WriteKey(BinaryWriter writer, string key)
	{
		writer.Write(key.Length);
		writer.Write(key.ToCharArray());
	}

	/// <summary>
	/// Validates the key identifier.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedKey">Expected key identifier.</param>
	/// <exception cref="InvalidDataException">Thrown when key doesn't match.</exception>
	private static void ValidateKey(BinaryReader reader, string expectedKey)
	{
		var keyLength = reader.ReadInt32();
		var keyChars = reader.ReadChars(keyLength);
		var actualKey = new string(keyChars);

		if (actualKey != expectedKey)
		{
			throw new InvalidDataException($"Expected key '{expectedKey}' but got '{actualKey}'");
		}
	}

	/// <summary>
	/// Validates the type indicator.
	/// </summary>
	/// <param name="reader">Binary reader.</param>
	/// <param name="expectedType">Expected type indicator.</param>
	/// <exception cref="InvalidDataException">Thrown when type doesn't match.</exception>
	private static void ValidateType(BinaryReader reader, TypeIndicator expectedType)
	{
		var actualType = (TypeIndicator)reader.ReadByte();

		if (actualType != expectedType)
		{
			throw new InvalidDataException($"Expected type '{expectedType}' but got '{actualType}'");
		}
	}
}
