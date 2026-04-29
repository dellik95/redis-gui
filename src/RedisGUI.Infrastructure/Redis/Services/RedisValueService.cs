using Microsoft.Extensions.Logging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;
using System.Linq;
using RedisValue = RedisGUI.Domain.Redis.RedisValue;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis value operations
/// </summary>
public class RedisValueService : IRedisValueService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<RedisValueService> logger;

	public RedisValueService(
		IConnectionPool connectionPool,
		ILogger<RedisValueService> logger)
	{
		this.connectionPool = connectionPool;
		this.logger = logger;
	}

	/// <summary>
	/// Sets a value in Redis
	/// </summary>
	public async Task<Result> SetValueAsync(
		RedisConnection connection,
		string key,
		string value,
		TimeSpan? expiry = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for set value: {Error}", multiplexerResult.Error.Message);
			return Result.Failure(multiplexerResult.Error);
		}

		IDatabase database = multiplexerResult.Value.GetDatabase(connection.DatabaseNumber.Value);
		bool success = await database.StringSetAsync(key, value, expiry);
		
		return success ? Result.Success() : Result.Failure(DomainErrors.Redis.UpdateFailed);
	}

	/// <summary>
	/// Gets a value from Redis by key
	/// </summary>
	public async Task<Result<RedisValue>> GetValueAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<RedisValue>(multiplexerResult.Error);
		}

		IDatabase database = multiplexerResult.Value.GetDatabase(connection.DatabaseNumber.Value);
		
		try
		{
			RedisType type = await database.KeyTypeAsync(key);
			Result<object?> valueResult = await GetValueByType(database, key, type);
			
			if (valueResult.IsFailure)
			{
				return Result.Failure<RedisValue>(valueResult.Error);
			}

			TimeSpan? ttl = await database.KeyTimeToLiveAsync(key);
			
			Result<RedisValue> redisValueResult = RedisValue.Create(
				key,
				valueResult.Value?.ToString() ?? string.Empty,
				type.ToString().ToLowerInvariant(),
				ttl);
			
			if (redisValueResult.IsFailure)
			{
				return Result.Failure<RedisValue>(redisValueResult.Error);
			}
			
			return Result.Success(redisValueResult.Value);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to get value for key {Key}", key);
			return Result.Failure<RedisValue>(DomainErrors.Redis.ValueRetrievalFailed);
		}
	}

	/// <summary>
	/// Gets all values from Redis with their keys
	/// </summary>
	public async Task<Result<PagedRedisValues>> GetAllValuesAsync(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default)
	{
		Result<IEnumerable<string>> keysResult = await GetKeysAsync(connection, cancellationToken);
		if (keysResult.IsFailure)
		{
			return Result.Failure<PagedRedisValues>(keysResult.Error);
		}

		List<string> allKeys = keysResult.Value
			.Where(k => MatchPattern(k, pattern))
			.ToList();

		int totalCount = allKeys.Count;
		IEnumerable<string> keysToProcess = allKeys;

		if (pageSize.HasValue && pageNumber.HasValue)
		{
			int skip = (pageNumber.Value - 1) * pageSize.Value;
			keysToProcess = allKeys.Skip(skip).Take(pageSize.Value);
		}

		List<RedisValue> values = [];
		
		foreach (string key in keysToProcess)
		{
			Result<RedisValue> valueResult = await GetValueAsync(connection, key, cancellationToken);
			if (valueResult.IsSuccess)
			{
				values.Add(valueResult.Value);
			}
		}

		PagedRedisValues pagedResult = PagedRedisValues.Create(
			values,
			totalCount,
			pageNumber ?? 1,
			pageSize ?? totalCount);
		
		return Result.Success(pagedResult);
	}

	private static bool MatchPattern(string key, string pattern)
	{
		if (pattern == "*")
		{
			return true;
		}

		// Simple glob pattern matching
		string regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
			.Replace("\\*", ".*")
			.Replace("\\?", ".") + "$";
			
		return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
	}

	/// <summary>
	/// Updates a value in Redis
	/// </summary>
	public async Task<Result> UpdateValueAsync(
		RedisConnection connection,
		string key,
		object value,
		string type,
		TimeSpan? ttl = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure(multiplexerResult.Error);
		}

		IDatabase database = multiplexerResult.Value.GetDatabase(connection.DatabaseNumber.Value);
		
		try
		{
			RedisType redisType = Enum.Parse<RedisType>(type, true);
			
			switch (redisType)
			{
				case RedisType.String:
					string stringValue = value.ToString() ?? string.Empty;
					await database.StringSetAsync(key, stringValue, ttl);
					break;
					
				case RedisType.List:
					if (value is not IEnumerable<string> listValues)
					{
						return Result.Failure(DomainErrors.Redis.InvalidValueType);
					}
					await database.KeyDeleteAsync(key);
					foreach (string item in listValues)
					{
						await database.ListRightPushAsync(key, item);
					}
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;
					
				case RedisType.Set:
					if (value is not IEnumerable<string> setValues)
					{
						return Result.Failure(DomainErrors.Redis.InvalidValueType);
					}
					await database.KeyDeleteAsync(key);
					foreach (string member in setValues)
					{
						await database.SetAddAsync(key, member);
					}
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;
					
				case RedisType.SortedSet:
					if (value is not IEnumerable<SortedSetEntry> sortedSetValues)
					{
						return Result.Failure(DomainErrors.Redis.InvalidValueType);
					}
					await database.KeyDeleteAsync(key);
					await database.SortedSetAddAsync(key, sortedSetValues.ToArray());
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;
					
				case RedisType.Hash:
					if (value is not Dictionary<string, string> hashValues)
					{
						return Result.Failure(DomainErrors.Redis.InvalidValueType);
					}
					await database.KeyDeleteAsync(key);
					HashEntry[] entries = hashValues.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray();
					await database.HashSetAsync(key, entries);
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;
					
				default:
					return Result.Failure(DomainErrors.Redis.UnsupportedKeyType);
			}
			
			return Result.Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to update value for key {Key}", key);
			return Result.Failure(DomainErrors.Redis.ValueUpdateFailed);
		}
	}

	#region Private Helper Methods

	private async Task<Result<object?>> GetValueByType(IDatabase database, string key, RedisType type)
	{
		try
		{
			return type switch
			{
				RedisType.String => Result.Success<object?>(await database.StringGetAsync(key)),
				RedisType.List => Result.Success<object?>(await database.ListRangeAsync(key)),
				RedisType.Set => Result.Success<object?>(await database.SetMembersAsync(key)),
				RedisType.SortedSet => Result.Success<object?>(await database.SortedSetRangeByRankWithScoresAsync(key)),
				RedisType.Hash => Result.Success<object?>(await database.HashGetAllAsync(key)),
				RedisType.Stream => Result.Success(await GetStreamValue(database, key)),
				RedisType.None => Result.Failure<object?>(DomainErrors.Redis.KeyNotFound),
				_ => Result.Failure<object?>(DomainErrors.Redis.UnsupportedKeyType)
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to get value by type for key {Key}", key);
			return Result.Failure<object?>(DomainErrors.Redis.ValueRetrievalFailed);
		}
	}

	private static async Task<object?> GetStreamValue(IDatabase database, string key)
	{
		StreamEntry[] entries = await database.StreamRangeAsync(key, count: 100);
		return entries.Select(entry => new
		{
			Id = entry.Id.ToString(),
			Values = entry.Values.ToDictionary(nv => nv.Name.ToString(), nv => nv.Value.ToString())
		}).ToArray();
	}

	private async ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for keys: {Error}", multiplexerResult.Error.Message);
			return Result.Failure<IEnumerable<string>>(multiplexerResult.Error);
		}

		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		IEnumerable<IServer> servers = multiplexer.GetServers().Where(s => s is { IsConnected: true, Features.Scan: true });
		List<string> keys = [];

		var result = servers
			.Select(server => server.Keys(connection.DatabaseNumber.Value).Select(x => x.ToString()))
			.Aggregate(keys, (current, serverKeys) => current.Union(serverKeys).ToList());
		
		return Result.Success<IEnumerable<string>>(result);
	}

	#endregion
}
