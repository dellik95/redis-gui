using System;
using RedisGUI.Infrastructure.Persistence.InMemoryDbDataReader;

namespace RedisGUI.Infrastructure.Persistence.Cache;

/// <summary>
/// Represents a cache entry that stores database query results
/// </summary>
[Serializable]
public class CacheEntry
{
	/// <summary>
	/// Gets or sets the table rows returned from a database query
	/// </summary>
	public DbTableRows TableRows { get; set; }

	/// <summary>
	/// Gets or sets the number of rows affected by a non-query operation
	/// </summary>
	public int NonQuery { get; set; }

	/// <summary>
	/// Gets or sets the scalar result from a database query
	/// </summary>
	public object Scalar { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the result is null
	/// </summary>
	public bool IsNull { get; set; }
}
