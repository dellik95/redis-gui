using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisValue = RedisGUI.Domain.Redis.RedisValue;
using RedisGUI.Infrastructure.Redis.Extensions;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis connections and operations
/// </summary>
public class ConnectionService : IConnectionService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<ConnectionService> logger;

	/// <summary>
	/// Create new instance of <see cref="ConnectionService"/>
	/// </summary>
	/// <param name="connectionPool">Connection pool service</param>
	/// <param name="logger">Logger service</param>
	public ConnectionService(
		IConnectionPool connectionPool,
		ILogger<ConnectionService> logger)
	{
		this.connectionPool = connectionPool;
		this.logger = logger;
	}

	/// <summary>
	/// Verifies if a Redis connection can be established
	/// </summary>
	public async Task<Result> CheckConnection(RedisConnection connection)
	{
		var multiplexerResult = await connectionPool.GetConnection(connection.ToConfigurationOptions());

		if (multiplexerResult.IsFailure)
		{
			return Result.Failure(multiplexerResult.Error);
		}

		var multiplexer = multiplexerResult.Value;
		var result = multiplexer.GetDatabase(connection.DatabaseNumber).Ping();

		return result.CompareTo(TimeSpan.Zero) > 0
			? Result.Success()
			: Result.Failure(DomainErrors.Connection.ConnectionNotEstablished);
	}

	/// <summary>
	/// Retrieves all keys from the Redis database
	/// </summary>
	public async ValueTask<IEnumerable<string>> GetKeys(RedisConnection connection)
	{
		var multiplexerResult = await connectionPool.GetConnection(connection.ToConfigurationOptions());
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for keys");
			throw new Exception("Failed to get connection for keys");
		}

		var multiplexer = multiplexerResult.Value;
		var servers = multiplexer.GetServers().Where(s => s is { IsConnected: true, Features.Scan: true });
		var keys = new List<string>();

		return servers.Select(server => server.Keys(connection.DatabaseNumber).Select(x => x.ToString())).Aggregate(keys, (current, serverKeys) => current.Union(serverKeys).ToList());
	}

	/// <summary>
	/// Deletes multiple keys from the Redis database
	/// </summary>
	/// <returns>Number of keys deleted</returns>
	public async Task<long> DeleteKeys(RedisConnection connection, string[] keys)
	{
		var multiplexerResult = await connectionPool.GetConnection(connection.ToConfigurationOptions());
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for delete keys");
			throw new Exception("Failed to get connection for delete keys");
		}

		var multiplexer = multiplexerResult.Value;
		var database = multiplexer.GetDatabase(connection.DatabaseNumber);

		var redisKeys = keys.Select(x => new RedisKey(x)).ToArray();
		var deletedCount = await database.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
		return deletedCount;
	}

	/// <summary>
	/// Sets a value for a specific key in Redis
	/// </summary>
	public async Task<Result> SetValue(RedisConnection connection, string key, string value, TimeSpan? ttl = null)
	{
		var multiplexerResult = await connectionPool.GetConnection(connection.ToConfigurationOptions());
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for set value");
			throw new Exception("Failed to get connection for set value");
		}

		var multiplexer = multiplexerResult.Value;
		var database = multiplexer.GetDatabase(connection.DatabaseNumber);

		if (ttl.HasValue)
		{
			await database.StringSetAsync(key, value, ttl.Value);
		}
		else
		{
			await database.StringSetAsync(key, value);
		}

		return Result.Success();
	}

	/// <summary>
	/// Retrieves a value for a specific key from Redis
	/// </summary>
	public async Task<Result<RedisValue>> GetValue(RedisConnection connection, string key)
	{
		var multiplexerResult = await connectionPool.GetConnection(connection.ToConfigurationOptions());
		var database = multiplexerResult.Value.GetDatabase(connection.DatabaseNumber);

		var type = await database.KeyTypeAsync(key);
		if (type == RedisType.None)
		{
			return Result.Failure<RedisValue>(new Error("Redis.KeyNotFound", "The specified key was not found"));
		}

		string value;
		switch (type)
		{
			case RedisType.String:
				value = await database.StringGetAsync(key);
				break;
			case RedisType.List:
				value = string.Join(",", await database.ListRangeAsync(key));
				break;
			case RedisType.Set:
				value = string.Join(",", await database.SetMembersAsync(key));
				break;
			case RedisType.Hash:
				value = string.Join(",", (await database.HashGetAllAsync(key)).Select(h => $"{h.Name}:{h.Value}"));
				break;
			case RedisType.SortedSet:
				value = string.Join(",", await database.SortedSetRangeByScoreWithScoresAsync(key));
				break;
			case RedisType.None:
			case RedisType.Stream:
			case RedisType.Unknown:
				throw new NotImplementedException();
			default:
				value = string.Empty;
				break;
		}

		var ttl = await database.KeyTimeToLiveAsync(key);

		return RedisValue.FromRedis(
			key,
			value.ToString(),
			type.ToString(),
			ttl);
	}

	/// <summary>
	/// Retrieves all values from Redis with optional pagination
	/// </summary>
	public async Task<Result<(IEnumerable<Domain.Connection.RedisValue> Values, int TotalCount, int PageNumber, int PageSize)>> GetAllValues(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null)
	{
		var multiplexer = await connectionPool.GetConnection(connection.ToConfigurationOptions());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);

		var server = multiplexer.Value.GetServer(connection.BuildConnectionString());
		var keys = server.Keys(database.Database, pattern ?? "*").ToList();

		var totalCount = keys.Count;
		var currentPage = pageNumber ?? 1;
		var itemsPerPage = pageSize ?? totalCount;

		var pagedKeys = keys
			.Skip((currentPage - 1) * itemsPerPage)
			.Take(itemsPerPage);

		var values = new List<RedisValue>();

		foreach (var key in pagedKeys)
		{
			var keyString = key.ToString();
			var type = await database.KeyTypeAsync(keyString);
			var value = await GetValueByType(database, keyString, type);
			var ttl = await database.KeyTimeToLiveAsync(keyString);

			values.Add(RedisValue.Create(
				keyString,
				value,
				type.ToString(),
				ttl));
		}

		return null;  //Result.Success(new (values, totalCount, currentPage, itemsPerPage));
	}

	private static async Task<string> GetValueByType(IDatabase database, string key, RedisType type)
	{
		return type switch
		{
			RedisType.String => await database.StringGetAsync(key),
			RedisType.List => string.Join(",", await database.ListRangeAsync(key)),
			RedisType.Set => string.Join(",", await database.SetMembersAsync(key)),
			RedisType.Hash => string.Join(",", (await database.HashGetAllAsync(key)).Select(h => $"{h.Name}:{h.Value}")),
			RedisType.SortedSet => string.Join(",", await database.SortedSetRangeByScoreAsync(key)),
			_ => string.Empty
		};
	}

	/// <summary>
	/// Deletes a specific key from Redis
	/// </summary>
	public async Task<Result<bool>> DeleteKey(RedisConnection connection, string key)
	{
		var multiplexer = await connectionPool.GetConnection(connection.ToConfigurationOptions());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);

		var deleted = await database.KeyDeleteAsync(key);
		return Result.Success(deleted);
	}

	/// <summary>
	/// Gets the type of value stored at a specific key
	/// </summary>
	public async Task<Result<string>> GetKeyType(RedisConnection connection, string key)
	{
		var multiplexer = await connectionPool.GetConnection(connection.ToConfigurationOptions());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);

		var type = await database.KeyTypeAsync(key);
		return Result.Success(type.ToString());
	}
}
