using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using RedisGUI.Infrastructure.Extensions;

namespace RedisGUI.Infrastructure.Persistence.InMemoryDbDataReader;

/// <summary>
/// Provides a data reader implementation for in-memory table rows
/// </summary>
public class InMemoryTableRowsDataReader : DbDataReader
{
	private readonly int rowsCount;
	private readonly DbTableRows tableRows;
	private readonly Dictionary<int, Type> valueTypes;
	private int currentRow;
	private bool isClosed;
	private IList<object> rowValues = new List<object>();

	/// <summary>
	/// Initializes a new instance of the InMemoryTableRowsDataReader
	/// </summary>
	/// <param name="tableRows">The table rows to read from</param>
	public InMemoryTableRowsDataReader(DbTableRows tableRows)
	{
		this.tableRows = tableRows ?? throw new ArgumentNullException(nameof(tableRows));
		rowsCount = this.tableRows.RowsCount;
		valueTypes = new Dictionary<int, Type>(this.tableRows.FieldCount);
	}

	/// <inheritdoc />
	public override int FieldCount => tableRows.FieldCount;

	/// <inheritdoc />
	public override bool HasRows => rowsCount > 0;

	/// <inheritdoc />
	public override bool IsClosed => isClosed;

	/// <inheritdoc />
	public override int Depth => tableRows.Get(currentRow)?.Depth ?? 0;

	/// <inheritdoc />
	public override int RecordsAffected => -1;

	/// <inheritdoc />
	public override object this[string name] => GetValue(GetOrdinal(name));

	/// <inheritdoc />
	public override object this[int ordinal] => GetValue(ordinal);

	/// <inheritdoc />
	public override string GetDataTypeName(int ordinal) => tableRows.GetDataTypeName(ordinal);

	/// <inheritdoc />
	public override IEnumerator GetEnumerator() => throw new NotSupportedException();

	/// <inheritdoc />
	public override Type GetFieldType(int ordinal) => tableRows.GetFieldType(ordinal);

	/// <inheritdoc />
	public override string GetName(int ordinal) => tableRows.GetName(ordinal);

	/// <inheritdoc />
	public override int GetOrdinal(string name) => tableRows.GetOrdinal(name);

	/// <inheritdoc />
	public override DataTable GetSchemaTable() => throw new InvalidOperationException();

	/// <inheritdoc />
	public override bool NextResult() => false;

	/// <inheritdoc />
	public override void Close() => isClosed = true;

	/// <inheritdoc />
	public override bool Read()
	{
		if (currentRow >= rowsCount)
		{
			return false;
		}

		rowValues = tableRows.Get(currentRow++).Values;

		return true;
	}

	/// <inheritdoc />
	public override bool GetBoolean(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return default;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.LongType)
		{
			return (long)value != 0;
		}

		if (valueType == TypeExtensions.UlongType)
		{
			return (ulong)value != 0;
		}

		if (valueType != TypeExtensions.BoolType)
		{
			return (ulong)Convert.ChangeType(value, TypeExtensions.UlongType, CultureInfo.InvariantCulture) != 0;
		}

		return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override byte GetByte(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return default;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.BoolType)
		{
			return (bool)value ? (byte)1 : (byte)0;
		}

		if (valueType == TypeExtensions.LongType)
		{
			return (byte)(long)value;
		}

		if (valueType != TypeExtensions.ByteType)
		{
			return (byte)Convert.ChangeType(value, TypeExtensions.ByteType, CultureInfo.InvariantCulture);
		}

		return Convert.ToByte(value, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => 0L;

	/// <inheritdoc />
	public override char GetChar(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return '\0';
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType != TypeExtensions.StringType)
		{
			return Convert.ToChar(value, CultureInfo.InvariantCulture);
		}

		var val = value.ToString();

		if (string.IsNullOrWhiteSpace(val))
		{
			return '\0';
		}

		return val[0];
	}

	/// <inheritdoc />
	public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => checked((char)GetInt64(ordinal));

	/// <inheritdoc />
	public override DateTime GetDateTime(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return default;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.DateTimeType)
		{
			return (DateTime)value;
		}

		var s = value.ToString();

		return string.IsNullOrWhiteSpace(s) ? default : DateTime.Parse(s, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override decimal GetDecimal(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return 0;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.StringType)
		{
			var s = value.ToString();

			return string.IsNullOrWhiteSpace(s) ? 0 : decimal.Parse(s, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
		}

		if (valueType != TypeExtensions.DecimalType)
		{
			return (decimal)Convert.ChangeType(value, TypeExtensions.DecimalType, CultureInfo.InvariantCulture);
		}

		return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override double GetDouble(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return 0;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType != TypeExtensions.DoubleType)
		{
			return (double)Convert.ChangeType(value, TypeExtensions.DoubleType, CultureInfo.InvariantCulture);
		}

		return Convert.ToDouble(value, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override float GetFloat(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return 0;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.DoubleType)
		{
			return (float)(double)value;
		}

		if (valueType != TypeExtensions.FloatType)
		{
			return (float)Convert.ChangeType(value, TypeExtensions.FloatType, CultureInfo.InvariantCulture);
		}

		return Convert.ToSingle(value, CultureInfo.InvariantCulture);
	}


	/// <inheritdoc />
	public override Guid GetGuid(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return Guid.NewGuid();
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.StringType)
		{
			var g = value.ToString();

			return string.IsNullOrWhiteSpace(g) ? Guid.NewGuid() : new Guid(g);
		}

		if (valueType == TypeExtensions.ByteArrayType)
		{
			return new Guid((byte[])value);
		}

		return (Guid)value;
	}


	/// <inheritdoc />
	public override short GetInt16(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return 0;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.BoolType)
		{
			return (bool)value ? (short)1 : (short)0;
		}

		if (valueType == TypeExtensions.LongType)
		{
			return (short)(long)value;
		}

		if (valueType != TypeExtensions.ShortType)
		{
			return (short)Convert.ChangeType(value, TypeExtensions.ShortType, CultureInfo.InvariantCulture);
		}

		return Convert.ToInt16(value, CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	public override int GetInt32(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return default;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.BoolType)
		{
			return (bool)value ? 1 : 0;
		}

		if (valueType == TypeExtensions.LongType)
		{
			return (int)(long)value;
		}

		if (valueType != TypeExtensions.IntType)
		{
			return (int)Convert.ChangeType(value, TypeExtensions.IntType, CultureInfo.InvariantCulture);
		}

		return Convert.ToInt32(value, CultureInfo.InvariantCulture);
	}


	/// <inheritdoc />
	public override long GetInt64(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value.IsNull())
		{
			return default;
		}

		var valueType = GetOrdinalValueType(ordinal, value);

		if (valueType == TypeExtensions.BoolType)
		{
			return (bool)value ? 1 : 0;
		}

		if (valueType != TypeExtensions.LongType)
		{
			return (long)Convert.ChangeType(value, TypeExtensions.LongType, CultureInfo.InvariantCulture);
		}

		return Convert.ToInt64(value, CultureInfo.InvariantCulture);
	}


	/// <inheritdoc />
	public override string GetString(int ordinal)
	{
		var value = GetValue(ordinal);

		return value.IsNull() ? string.Empty : value.ToString() ?? string.Empty;
	}


	/// <inheritdoc />
	public override object GetValue(int ordinal) => rowValues[ordinal];


	/// <inheritdoc />
	public override int GetValues(object[] values)
	{
		Array.Copy(rowValues.ToArray(), values, rowValues.Count);

		return rowValues.Count;
	}


	/// <inheritdoc />
	public override bool IsDBNull(int ordinal)
	{
		var value = rowValues[ordinal];

		return value.IsNull();
	}

	private Type GetOrdinalValueType(int ordinal, object value)
	{
		if (valueTypes.TryGetValue(ordinal, out var valueType))
		{
			return valueType;
		}

		valueType = value.GetType();
		valueTypes.Add(ordinal, valueType);

		return valueType;
	}
}
