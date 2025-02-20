using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace RedisGUI.Infrastructure.Persistence.Cache;

/// <summary>
/// Provides functionality to generate cache keys for database queries
/// </summary>
public interface ICacheKeyProvider
{
	/// <summary>
	/// Generates a cache key based on the database command, context and table names
	/// </summary>
	/// <param name="command">The database command</param>
	/// <param name="context">The database context</param>
	/// <param name="tableNames">The set of table names involved in the query</param>
	/// <returns>A unique cache key string</returns>
	/// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
	string GetCacheKey(DbCommand command, DbContext context, SortedSet<string> tableNames);
}
