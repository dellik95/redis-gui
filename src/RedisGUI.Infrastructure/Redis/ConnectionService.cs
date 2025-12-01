using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RedisValue = RedisGUI.Domain.Redis.RedisValue;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis connections and operations
/// </summary>
public class ConnectionService : IConnectionService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<ConnectionService> logger;
	private readonly Dictionary<string, double> lastValues = [];
	
	// Pub/Sub subscription tracking
	private readonly ConcurrentDictionary<string, (ISubscriber Subscriber, PubSubSubscription Subscription)> subscriptions = new();

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
		var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());

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
		var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
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
		var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
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
		var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
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
		var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
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
			case RedisType.Stream:
				var streamEntries = await database.StreamReadAsync(key, "0-0");
				value = string.Join(";", streamEntries.Select(entry => 
					$"{entry.Id}:[{string.Join(",", entry.Values.Select(v => $"{v.Name}={v.Value}"))}]"));
				break;
			case RedisType.None:
			case RedisType.Unknown:
				value = string.Empty;
				logger.LogWarning("Key {Key} has type {Type} which is not supported for value retrieval", key, type);
				break;
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
	public async Task<Result<PagedRedisValues>> GetAllValues(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null)
	{
		var multiplexer = await connectionPool.GetConnection(connection.BuildConnectionString());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);

		var server = multiplexer.Value.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);
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

			var redisValue = RedisValue.Create(
				keyString,
				value,
				type.ToString(),
				ttl);

			if (redisValue.IsSuccess)
			{
				values.Add(redisValue.Value);
			}
		}

		var pagedResult = PagedRedisValues.Create(
			values,
			totalCount,
			currentPage,
			itemsPerPage);

		return Result.Success(pagedResult);
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
			RedisType.Stream => await GetStreamValue(database, key),
			_ => string.Empty
		};
	}

	private static async Task<string> GetStreamValue(IDatabase database, string key)
	{
		var streamEntries = await database.StreamReadAsync(key, "0-0");
		return string.Join(";", streamEntries.Select(entry => 
			$"{entry.Id}:[{string.Join(",", entry.Values.Select(v => $"{v.Name}={v.Value}"))}]"));
	}

	/// <summary>
	/// Deletes a specific key from Redis
	/// </summary>
	public async Task<Result<bool>> DeleteKey(RedisConnection connection, string key)
	{
		var multiplexer = await connectionPool.GetConnection(connection.BuildConnectionString());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);

		var deleted = await database.KeyDeleteAsync(key);
		return Result.Success(deleted);
	}

	/// <summary>
	/// Delete multiple keys from redis by their exact names
	/// </summary>
	public async Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeys(
		RedisConnection connection,
		IReadOnlyList<string> keys)
	{
		var multiplexer = await connectionPool.GetConnection(connection.BuildConnectionString());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);

		var failedKeys = new List<string>();
		var deletedCount = 0;

		foreach (var key in keys)
		{
			try
			{
				var deleted = await database.KeyDeleteAsync(key);
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
	public async Task<Result<int>> DeleteKeysByPattern(RedisConnection connection, string pattern)
	{
		var multiplexer = await connectionPool.GetConnection(connection.BuildConnectionString());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);
		var server = multiplexer.Value.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);

		try
		{
			var keys = server.Keys(database.Database, pattern).ToArray();
			
			if (keys.Length == 0)
			{
				return Result.Success(0);
			}

			var deletedCount = await database.KeyDeleteAsync(keys);
			return Result.Success((int)deletedCount);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to delete keys by pattern {Pattern}", pattern);
			return Result.Failure<int>(new Error("Redis.DeleteByPatternFailed", $"Failed to delete keys by pattern: {ex.Message}"));
		}
	}

	/// <summary>
	/// Gets the type of value stored at a specific key
	/// </summary>
	public async Task<Result<string>> GetKeyType(RedisConnection connection, string key)
	{
		var multiplexer = await connectionPool.GetConnection(connection.BuildConnectionString());
		var database = multiplexer.Value.GetDatabase(connection.DatabaseNumber);

		var type = await database.KeyTypeAsync(key);
		return Result.Success(type.ToString());
	}

	/// <summary>
	/// Updates a Redis value with type-specific logic
	/// </summary>
	public async Task<Result> UpdateValue(RedisConnection connection, string key, object value, string type, TimeSpan? ttl = null)
	{
		var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for update value");
			return Result.Failure(multiplexerResult.Error);
		}

		var multiplexer = multiplexerResult.Value;
		var database = multiplexer.GetDatabase(connection.DatabaseNumber);

		try
		{
			// Parse the type
			if (!Enum.TryParse<RedisType>(type, true, out var redisType))
			{
				return Result.Failure(new Error("Redis.InvalidType", $"Invalid Redis type: {type}"));
			}

			// Type-specific update logic
			switch (redisType)
			{
				case RedisType.String:
					if (value is not string stringValue)
					{
						return Result.Failure(new Error("Redis.InvalidValue", "String type requires string value"));
					}
					await database.StringSetAsync(key, stringValue, ttl);
					break;

				case RedisType.List:
					if (value is not IEnumerable<string> listItems)
					{
						return Result.Failure(new Error("Redis.InvalidValue", "List type requires IEnumerable<string> value"));
					}
					// Delete existing list and recreate
					await database.KeyDeleteAsync(key);
					if (listItems.Any())
					{
						var redisValues = listItems.Select(x => (StackExchange.Redis.RedisValue)x).ToArray();
						await database.ListRightPushAsync(key, redisValues);
					}
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;

				case RedisType.Set:
					if (value is not IEnumerable<string> setMembers)
					{
						return Result.Failure(new Error("Redis.InvalidValue", "Set type requires IEnumerable<string> value"));
					}
					// Delete existing set and recreate
					await database.KeyDeleteAsync(key);
					if (setMembers.Any())
					{
						var redisValues = setMembers.Select(x => (StackExchange.Redis.RedisValue)x).ToArray();
						await database.SetAddAsync(key, redisValues);
					}
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;

				case RedisType.Hash:
					if (value is not Dictionary<string, string> hashFields)
					{
						return Result.Failure(new Error("Redis.InvalidValue", "Hash type requires Dictionary<string, string> value"));
					}
					// Delete existing hash and recreate
					await database.KeyDeleteAsync(key);
					if (hashFields.Count > 0)
					{
						var entries = hashFields.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
						await database.HashSetAsync(key, entries);
					}
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;

				case RedisType.SortedSet:
					if (value is not Dictionary<string, double> sortedSetMembers)
					{
						return Result.Failure(new Error("Redis.InvalidValue", "SortedSet type requires Dictionary<string, double> value"));
					}
					// Delete existing sorted set and recreate
					await database.KeyDeleteAsync(key);
					if (sortedSetMembers.Count > 0)
					{
						var entries = sortedSetMembers.Select(kv => new SortedSetEntry(kv.Key, kv.Value)).ToArray();
						await database.SortedSetAddAsync(key, entries);
					}
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;

				case RedisType.Stream:
					if (value is not List<Dictionary<string, string>> streamEntries)
					{
						return Result.Failure(new Error("Redis.InvalidValue", "Stream type requires List<Dictionary<string, string>> value"));
					}
					// Streams are append-only, so we add new entries
					foreach (var entry in streamEntries)
					{
						var nameValues = entry.Select(kv => new NameValueEntry(kv.Key, kv.Value)).ToArray();
						await database.StreamAddAsync(key, nameValues);
					}
					if (ttl.HasValue)
					{
						await database.KeyExpireAsync(key, ttl.Value);
					}
					break;

				default:
					return Result.Failure(new Error("Redis.UnsupportedType", $"Unsupported Redis type: {redisType}"));
			}

			return Result.Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error updating Redis value for key {Key}", key);
			return Result.Failure(new Error("Redis.UpdateFailed", $"Failed to update value: {ex.Message}"));
		}
	}

	/// <summary>
	/// Publishes a message to a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result> PublishMessage(RedisConnection connection, string channel, string message)
	{
		try
		{
			var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure(multiplexerResult.Error);
			}

			var multiplexer = multiplexerResult.Value;
			var subscriber = multiplexer.GetSubscriber();

			var recipients = await subscriber.PublishAsync(channel, message);
			logger.LogInformation("Published message to channel {Channel}, received by {Recipients} subscribers", channel, recipients);

			return Result.Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error publishing message to channel {Channel}", channel);
			return Result.Failure(new Error("Redis.PublishFailed", $"Failed to publish message: {ex.Message}"));
		}
	}

	/// <summary>
	/// Subscribes to a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result<string>> SubscribeToChannel(
		RedisConnection connection,
		string channel,
		CancellationToken cancellationToken)
	{
		try
		{
			var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure<string>(multiplexerResult.Error);
			}

			var multiplexer = multiplexerResult.Value;
			var subscriber = multiplexer.GetSubscriber();

			var subscription = PubSubSubscription.Create(Guid.Empty, channel); // ConnectionId will be set by handler

			await subscriber.SubscribeAsync(channel, (ch, message) =>
			{
				logger.LogInformation("Received message on channel {Channel}: {Message}", ch, message);
				subscription.MessagesReceived++;
				// TODO: Broadcast via SignalR
			});

			subscriptions.TryAdd(subscription.SubscriptionId, (subscriber, subscription));

			logger.LogInformation("Subscribed to channel {Channel} with ID {SubscriptionId}", channel, subscription.SubscriptionId);
			return Result.Success(subscription.SubscriptionId);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error subscribing to channel {Channel}", channel);
			return Result.Failure<string>(new Error("Redis.SubscribeFailed", $"Failed to subscribe: {ex.Message}"));
		}
	}

	/// <summary>
	/// Unsubscribes from a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result> UnsubscribeFromChannel(
		RedisConnection connection,
		string subscriptionId,
		CancellationToken cancellationToken)
	{
		try
		{
			if (!subscriptions.TryRemove(subscriptionId, out var sub))
			{
				return Result.Failure(new Error("Redis.SubscriptionNotFound", "Subscription not found"));
			}

			await sub.Subscriber.UnsubscribeAsync(sub.Subscription.Channel);
			logger.LogInformation("Unsubscribed from channel {Channel}", sub.Subscription.Channel);

			return Result.Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error unsubscribing from subscription {SubscriptionId}", subscriptionId);
			return Result.Failure(new Error("Redis.UnsubscribeFailed", $"Failed to unsubscribe: {ex.Message}"));
		}
	}

	/// <summary>
	/// Executes a Redis transaction (MULTI/EXEC)
	/// </summary>
	public async Task<Result<List<string>>> ExecuteTransaction(
		RedisConnection connection,
		List<RedisOperation> operations)
	{
		try
		{
			var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure<List<string>>(multiplexerResult.Error);
			}

			var multiplexer = multiplexerResult.Value;
			var database = multiplexer.GetDatabase(connection.DatabaseNumber);
			var transaction = database.CreateTransaction();

			var tasks = new List<Task<StackExchange.Redis.RedisValue>>();

			foreach (var op in operations)
			{
				switch (op.Command.ToUpperInvariant())
				{
					case "SET":
						if (!string.IsNullOrEmpty(op.Value))
						{
							var setTask = transaction.StringSetAsync(op.Key, op.Value);
							tasks.Add(setTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						}
						break;

					case "GET":
						var getTask = transaction.StringGetAsync(op.Key);
						tasks.Add(getTask);
						break;

					case "DEL":
						var delTask = transaction.KeyDeleteAsync(op.Key);
						tasks.Add(delTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						break;

					case "INCR":
						var incrTask = transaction.StringIncrementAsync(op.Key);
						tasks.Add(incrTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						break;

					case "DECR":
						var decrTask = transaction.StringDecrementAsync(op.Key);
						tasks.Add(decrTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						break;

					default:
						logger.LogWarning("Unsupported transaction command: {Command}", op.Command);
						break;
				}
			}

			var committed = await transaction.ExecuteAsync();

			if (!committed)
			{
				return Result.Failure<List<string>>(new Error("Redis.TransactionFailed", "Transaction was not committed"));
			}

			await Task.WhenAll(tasks);

			var results = tasks.Select(t => t.Result.ToString()).ToList();
			return Result.Success(results);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error executing transaction");
			return Result.Failure<List<string>>(new Error("Redis.TransactionFailed", $"Transaction failed: {ex.Message}"));
		}
	}

	/// <summary>
	/// Executes a Lua script in Redis using EVAL with SHA-based caching
	/// </summary>
	public async Task<Result<(string Result, string ScriptHash)>> ExecuteLuaScript(
		RedisConnection connection,
		string script,
		string[] keys,
		string[] arguments)
	{
		try
		{
			var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString());
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure<(string, string)>(multiplexerResult.Error);
			}

			var multiplexer = multiplexerResult.Value;
			var database = multiplexer.GetDatabase(connection.DatabaseNumber);

			// Generate SHA1 hash for the script
			var scriptHash = ComputeSha1(script);

			// Convert keys and arguments to RedisKey and RedisValue
			var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
			var redisValues = arguments.Select(a => (StackExchange.Redis.RedisValue)a).ToArray();

			StackExchange.Redis.RedisValue result;

			try
			{
				// Try to execute using cached script hash (EVALSHA)
				var evalResult = await database.ScriptEvaluateAsync(scriptHash, redisKeys, redisValues);
				result = ConvertRedisResultToValue(evalResult);
			}
			catch (RedisServerException ex) when (ex.Message.Contains("NOSCRIPT"))
			{
				// Script not cached, use EVAL
				var evalResult = await database.ScriptEvaluateAsync(script, redisKeys, redisValues);
				result = ConvertRedisResultToValue(evalResult);
			}

			return Result.Success((result.ToString(), scriptHash));
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error executing Lua script");
			return Result.Failure<(string, string)>(new Error("Redis.ScriptFailed", $"Script execution failed: {ex.Message}"));
		}
	}

	private static string ComputeSha1(string input)
	{
		var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	private static StackExchange.Redis.RedisValue ConvertRedisResultToValue(RedisResult result)
	{
		// RedisResult can be converted to RedisValue
		if (result.IsNull)
		{
			return StackExchange.Redis.RedisValue.Null;
		}

		return (StackExchange.Redis.RedisValue)result;
	}
}
