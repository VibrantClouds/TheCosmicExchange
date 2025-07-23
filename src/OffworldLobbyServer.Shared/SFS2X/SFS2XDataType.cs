namespace OffworldLobbyServer.Shared.SFS2X;

/// <summary>
/// SFS2X data type constants based on SmartFoxServer 2X protocol specification.
/// These exact numeric values are required for client compatibility.
/// </summary>
public enum SFS2XDataType : byte
{
	/// <summary>
	/// Null value.
	/// </summary>
	NULL = 0,

	/// <summary>
	/// Boolean value.
	/// </summary>
	BOOL = 1,

	/// <summary>
	/// Byte value, signed 8 bits.
	/// </summary>
	BYTE = 2,

	/// <summary>
	/// Short integer, signed 16 bits.
	/// </summary>
	SHORT = 3,

	/// <summary>
	/// Integer, signed 32 bits.
	/// </summary>
	INT = 4,

	/// <summary>
	/// Long integer, signed 64 bits.
	/// </summary>
	LONG = 5,

	/// <summary>
	/// Floating point decimal, signed 32 bits.
	/// </summary>
	FLOAT = 6,

	/// <summary>
	/// Double precision decimal, signed 64 bits.
	/// </summary>
	DOUBLE = 7,

	/// <summary>
	/// UTF-8 encoded string, with length up to 32 KBytes.
	/// </summary>
	UTF_STRING = 8,

	/// <summary>
	/// Array of booleans.
	/// </summary>
	BOOL_ARRAY = 9,

	/// <summary>
	/// Array of bytes (treated as ByteArray).
	/// </summary>
	BYTE_ARRAY = 10,

	/// <summary>
	/// Array of shorts.
	/// </summary>
	SHORT_ARRAY = 11,

	/// <summary>
	/// Array of integers.
	/// </summary>
	INT_ARRAY = 12,

	/// <summary>
	/// Array of long integers.
	/// </summary>
	LONG_ARRAY = 13,

	/// <summary>
	/// Array of floats.
	/// </summary>
	FLOAT_ARRAY = 14,

	/// <summary>
	/// Array of doubles.
	/// </summary>
	DOUBLE_ARRAY = 15,

	/// <summary>
	/// Array of UTF-8 strings.
	/// </summary>
	UTF_STRING_ARRAY = 16,

	/// <summary>
	/// SFSArray type.
	/// </summary>
	SFS_ARRAY = 17,

	/// <summary>
	/// SFSObject type.
	/// </summary>
	SFS_OBJECT = 18,

	/// <summary>
	/// Serialized class instance.
	/// </summary>
	CLASS = 19,

	/// <summary>
	/// UTF-8 encoded string, with length up to 2 GBytes.
	/// </summary>
	TEXT = 20
}