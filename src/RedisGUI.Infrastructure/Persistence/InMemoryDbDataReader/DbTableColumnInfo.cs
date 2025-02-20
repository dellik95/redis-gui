using System;
using System.Runtime.Serialization;

namespace RedisGUI.Infrastructure.Persistence.InMemoryDbDataReader;

/// <summary>
/// Represents metadata information about a database table column
/// </summary>
[Serializable]
[DataContract]
public class DbTableColumnInfo
{
	/// <summary>
	/// Gets or sets the ordinal position of the column
	/// </summary>
	[DataMember]
	public int Ordinal { get; set; }

	/// <summary>
	/// Gets or sets the name of the column
	/// </summary>
	[DataMember]
	public string Name { get; set; } = default!;

	/// <summary>
	/// Gets or sets the database type name of the column
	/// </summary>
	[DataMember]
	public string DbTypeName { get; set; } = default!;

	/// <summary>
	/// Gets or sets the .NET type name of the column
	/// </summary>
	[DataMember]
	public string TypeName { get; set; } = default!;
}
