using Microsoft.Extensions.Logging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis key operations
/// </summary>
public class RedisKeyService : IRedisKeyService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<RedisKeyService> logger;

	public RedisKeyService(
		IConnectionPool connectionPool,
		ILogger<RedisKeyService> logger)
	{
		this.connectionPool = connectionPool;
		this.logger = logger;
	}

	/// <summary>
	/// Retrieves all keys from the Redis database
	/// </summary>
	public async ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default)
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

	/// <summary>
	/// Deletes a specific key from Redis
	/// </summary>
	public async Task<Result<bool>> DeleteKeyAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<bool>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		bool deleted = await db.KeyDeleteAsync(key);
		
		return Result.Success(deleted);
	}

	/// <summary>
	/// Deletes multiple keys from the Redis database
	/// </summary>
	public async Task<Result<long>> DeleteKeysAsync(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for delete keys: {Error}", multiplexerResult.Error.Message);
			return Result.Failure<long>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		RedisKey[] redisKeys = keys.Select(x => new RedisKey(x)).ToArray();
		long deletedCount = await db.KeyDeleteAsync(redisKeys);
		
		return Result.Success(deletedCount);
	}

	/// <summary>
	/// Delete multiple keys from redis by their exact names with detailed results
	/// </summary>
	public async Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<(int, IReadOnlyList<string>)>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		List<string> failedKeys = [];
		int deletedCount = 0;

		foreach (string key in keys)
		{
			try
			{
				bool deleted = await db.KeyDeleteAsync(key);
				if (deleted)
				{
					deletedCount++;
				}
				else
				{
					failedKeys.Add(key);
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to delete key {Key}", key);
				failedKeys.Add(key);
			}
		}

		return Result.Success((deletedCount, (IReadOnlyList<string>)failedKeys));
	}

	/// <summary>
	/// Delete keys from redis matching a pattern
	/// </summary>
	public async Task<Result<int>> DeleteKeysByPatternAsync(RedisConnection connection, string pattern, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<int>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		IDatabase db = multiplexer.GetDatabase(targetDb);
		IServer server = multiplexer.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);

		try
		{
			RedisKey[] keys = server.Keys(targetDb, pattern).ToArray();

			if (keys.Length == 0)
			{
				return Result.Success(0);
			}

			long deletedCount = await db.KeyDeleteAsync(keys);
			return Result.Success((int)deletedCount);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to delete keys by pattern {Pattern}", pattern);
			return Result.Failure<int>(DomainErrors.Redis.DeleteByPatternFailed);
		}
	}

	/// <summary>
	/// Gets the type of value stored at a specific key
	/// </summary>
	public async Task<Result<string>> GetKeyTypeAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<string>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		RedisType type = await db.KeyTypeAsync(key);
		
		return Result.Success(type.ToString());
	}
}
