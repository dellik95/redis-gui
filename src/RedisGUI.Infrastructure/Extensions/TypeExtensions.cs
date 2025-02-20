using System;

namespace RedisGUI.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods and type constants for common data types
/// </summary>
public static class TypeExtensions
{
	/// <summary>
	/// Represents the System.Int64 type
	/// </summary>
	public static readonly Type LongType = typeof(long);

	/// <summary>
	/// Represents the System.UInt64 type
	/// </summary>
	public static readonly Type UlongType = typeof(ulong);

	/// <summary>
	/// Represents the System.Boolean type
	/// </summary>
	public static readonly Type BoolType = typeof(bool);

	/// <summary>
	/// Represents the System.Byte type
	/// </summary>
	public static readonly Type ByteType = typeof(byte);

	/// <summary>
	/// Represents the System.String type
	/// </summary>
	public static readonly Type StringType = typeof(string);

	/// <summary>
	/// Represents the System.DateTime type
	/// </summary>
	public static readonly Type DateTimeType = typeof(DateTime);

	/// <summary>
	/// Represents the System.Decimal type
	/// </summary>
	public static readonly Type DecimalType = typeof(decimal);

	/// <summary>
	/// Represents the System.Double type
	/// </summary>
	public static readonly Type DoubleType = typeof(double);

	/// <summary>
	/// Represents the System.Single type
	/// </summary>
	public static readonly Type FloatType = typeof(float);

	/// <summary>
	/// Represents the System.Byte[] type
	/// </summary>
	public static readonly Type ByteArrayType = typeof(byte[]);

	/// <summary>
	/// Represents the System.Int16 type
	/// </summary>
	public static readonly Type ShortType = typeof(short);

	/// <summary>
	/// Represents the System.Int32 type
	/// </summary>
	public static readonly Type IntType = typeof(int);

	/// <summary>
	/// Represents the System.TimeSpan type
	/// </summary>
	public static readonly Type TimeSpanType = typeof(TimeSpan);

	/// <summary>
	/// Represents the System.Char type
	/// </summary>
	public static readonly Type CharType = typeof(char);

	/// <summary>
	/// Determines whether the specified value is null or DBNull
	/// </summary>
	/// <param name="value">The value to check</param>
	/// <returns>True if the value is null or DBNull; otherwise, false</returns>
	public static bool IsNull(this object value) => value is null or DBNull;
}
