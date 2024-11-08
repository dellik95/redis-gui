using RedisGUI.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represent connection service
/// </summary>
public interface IConnectionService
{
	/// <summary>
	/// Check connection is available
	/// </summary>
	/// <param name="connection">Connection</param>
	/// <returns></returns>
	Task<Result> CheckConnection(RedisConnection connection);

	/// <summary>
	/// Returns all keys from redis
	/// </summary>
	/// <param name="connection"></param>
	/// <returns></returns>
	ValueTask<IEnumerable<string>> GetKeys(RedisConnection connection);

	/// <summary>
	/// Delete keys from redis
	/// </summary>
	/// <param name="connection">Redis connection></param>
	/// <param name="keys">Keys to delete</param>
	/// <returns></returns>
	Task<long> DeleteKeys(RedisConnection connection, string[] keys);

	/// <summary>
	/// Set a value in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <param name="value">Value</param>
	/// <param name="ttl">Time to live</param>
	/// <returns></returns>
	Task<Result> SetValue(RedisConnection connection, string key, string value, TimeSpan? ttl = null);

	/// <summary>
	/// Get a value from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <returns></returns>
	Task<Result<Redis.RedisValue>> GetValue(RedisConnection connection, string key);

	/// <summary>
	/// Get all values from redis with pagination and pattern matching
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="pattern">Pattern to match keys</param>
	/// <param name="pageSize">Number of items per page</param>
	/// <param name="pageNumber">Page number</param>
	/// <returns></returns>
	Task<Result<(IEnumerable<RedisValue> Values, int TotalCount, int PageNumber, int PageSize)>> GetAllValues(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null);

	/// <summary>
	/// Delete a key from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to delete</param>
	/// <returns></returns>
	Task<Result<bool>> DeleteKey(RedisConnection connection, string key);

	/// <summary>
	/// Get the type of a key in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to get the type of</param>
	/// <returns></returns>
	Task<Result<string>> GetKeyType(RedisConnection connection, string key);
}
