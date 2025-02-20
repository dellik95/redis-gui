using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.Serialization;

namespace RedisGUI.Infrastructure.Persistence.InMemoryDbDataReader;

/// <summary>
/// Represents a collection of database table rows and their metadata
/// </summary>
[Serializable]
[DataContract]
public class DbTableRows
{
    /// <summary>
    /// Initializes a new instance of DbTableRows from a DbDataReader
    /// </summary>
    /// <param name="reader">The data reader to initialize from</param>
    /// <exception cref="ArgumentNullException">Thrown when reader is null</exception>
    public DbTableRows(DbDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ColumnsInfo = new Dictionary<int, DbTableColumnInfo>(reader.FieldCount);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            ColumnsInfo.Add(i, new DbTableColumnInfo
            {
                Ordinal = i,
                Name = reader.GetName(i),
                DbTypeName = reader.GetDataTypeName(i),
                TypeName = reader.GetFieldType(i)?.ToString() ?? typeof(string).ToString()
            });
        }
    }

    /// <summary>
    /// Initializes a new instance of DbTableRows
    /// </summary>
    public DbTableRows() => ColumnsInfo = new Dictionary<int, DbTableColumnInfo>();

    /// <summary>
    /// Gets or sets the collection of table rows
    /// </summary>
    [DataMember]
    public IList<DbTableRow> Rows { get; set; } = new List<DbTableRow>();

    /// <summary>
    /// Gets or sets the column information dictionary
    /// </summary>
    [DataMember]
    public IDictionary<int, DbTableColumnInfo> ColumnsInfo { get; set; }

    /// <summary>
    /// Gets or sets the total number of fields
    /// </summary>
    [DataMember]
    public int FieldCount { get; set; }

    /// <summary>
    /// Gets or sets the number of visible fields
    /// </summary>
    [DataMember]
    public int VisibleFieldCount { get; set; }

    /// <summary>
    /// Gets the total number of rows
    /// </summary>
    [DataMember]
    public int RowsCount => Rows?.Count ?? 0;

    /// <summary>
    /// Adds a row to the collection
    /// </summary>
    /// <param name="item">The row to add</param>
    public void Add(DbTableRow item)
    {
        if (item != null)
        {
            Rows.Add(item);
        }
    }

    /// <summary>
    /// Gets a row at the specified index
    /// </summary>
    /// <param name="index">The zero-based index of the row</param>
    /// <returns>The row at the specified index</returns>
    public DbTableRow Get(int index) => Rows[index];

    /// <summary>
    /// Gets the ordinal position of a column by its name
    /// </summary>
    /// <param name="name">The name of the column</param>
    /// <returns>The ordinal position of the column</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the column name is not found</exception>
    public int GetOrdinal(string name)
    {
        var keyValuePair = ColumnsInfo.FirstOrDefault(pair => pair.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (keyValuePair.Value != null)
        {
            return keyValuePair.Value.Ordinal;
        }

        throw new ArgumentOutOfRangeException(nameof(name), name);
    }

    /// <summary>
    /// Gets the name of a column by its ordinal position
    /// </summary>
    /// <param name="ordinal">The ordinal position of the column</param>
    /// <returns>The name of the column</returns>
    public string GetName(int ordinal) => GetColumnInfo(ordinal).Name;

    /// <summary>
    /// Gets the database type name of a column by its ordinal position
    /// </summary>
    /// <param name="ordinal">The ordinal position of the column</param>
    /// <returns>The database type name of the column</returns>
    public string GetDataTypeName(int ordinal) => GetColumnInfo(ordinal).DbTypeName;

    /// <summary>
    /// Gets the .NET Type of a column by its ordinal position
    /// </summary>
    /// <param name="ordinal">The ordinal position of the column</param>
    /// <returns>The .NET Type of the column</returns>
    public Type GetFieldType(int ordinal) => Type.GetType(GetColumnInfo(ordinal).TypeName) ?? typeof(string);

    /// <summary>
    /// Gets the type name of a column by its ordinal position
    /// </summary>
    /// <param name="ordinal">The ordinal position of the column</param>
    /// <returns>The type name of the column</returns>
    public string GetFieldTypeName(int ordinal) => GetColumnInfo(ordinal).TypeName;

    private DbTableColumnInfo GetColumnInfo(int ordinal)
    {
        var dbColumnInfo = ColumnsInfo[ordinal];
        if (dbColumnInfo != null)
        {
            return dbColumnInfo;
        }

        throw new ArgumentOutOfRangeException(nameof(ordinal), $"Index[{ordinal}] was outside of array's bounds.");
    }
} 