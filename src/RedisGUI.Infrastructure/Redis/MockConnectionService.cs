using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.SignalR;
using RedisGUI.Infrastructure.SignalR.Hubs;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Comprehensive in-memory mock implementation of IConnectionService for backend-only mocking.
/// Supports all Redis data types, Pub/Sub, transactions, Lua scripts, and TTL expiration.
/// </summary>
public sealed partial class MockConnectionService : IConnectionService
{
	[GeneratedRegex(@"redis\.call\(['""]([^'""]+)['""],\s*['""]([^'""]+)['""]\)")]
	private static partial Regex RedisCallRegex();

	[GeneratedRegex(@"redis\.call\(['""]SET['""],\s*['""][^'""]+['""],\s*['""]([^'""]+)['""]\)")]
	private static partial Regex SetValueRegex();

	private readonly ConcurrentDictionary<string, MockRedisValue> store = new();
	private readonly ConcurrentDictionary<string, PubSubSubscription> subscriptions = new();
	private readonly ConcurrentDictionary<string, List<string>> pubSubChannels = new();
	private readonly IPubSubNotificationService? pubSubNotificationService;
	private readonly ILogger<MockConnectionService>? logger;
	private readonly Timer? ttlCleanupTimer;
	private readonly RedisMockOptions options;

	/// <summary>
	/// Initializes a new instance of MockConnectionService
	/// </summary>
	public MockConnectionService(
		IOptions<RedisMockOptions> options,
		IPubSubNotificationService? pubSubNotificationService = null,
		ILogger<MockConnectionService>? logger = null)
	{
		this.options = options.Value;
		this.pubSubNotificationService = pubSubNotificationService;
		this.logger = logger;

		// Seed dummy data if enabled
		if (this.options.SeedDummyData)
		{
			SeedDummyData();
		}

		// Start TTL cleanup timer
		if (this.options.EnableTtlExpiration)
		{
			ttlCleanupTimer = new Timer(ExpireKeysWithTtl, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
		}
	}

	public Task<Result> CheckConnection(RedisConnection connection, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());

	public ValueTask<Result<IEnumerable<string>>> GetKeys(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		IEnumerable<string> keys = store.Keys.AsEnumerable();
		return ValueTask.FromResult(Result.Success(keys));
	}

	public Task<Result<long>> DeleteKeys(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default)
	{
		long deleted = 0;
		foreach (string key in keys)
		{
			if (store.TryRemove(key, out _))
			{
				deleted++;
			}
		}
		return Task.FromResult(Result.Success(deleted));
	}

	public Task<Result> SetValue(RedisConnection connection, string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
	{
		store[key] = new MockRedisValue
		{
			Type = "String",
			StringValue = value,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
		};
		return Task.FromResult(Result.Success());
	}

	public Task<Result<RedisValue>> GetValue(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		if (!store.TryGetValue(key, out MockRedisValue? entry) || IsExpired(entry))
		{
			return Task.FromResult(Result.Failure<RedisValue>(DomainErrors.Redis.KeyNotFound));
		}

		string value = entry.Type switch
		{
			"String" => entry.StringValue ?? string.Empty,
			"List" => string.Join(",", entry.ListValue ?? []),
			"Set" => string.Join(",", entry.SetValue ?? []),
			"Hash" => string.Join(",", entry.HashValue?.Select(kv => $"{kv.Key}:{kv.Value}") ?? []),
			"SortedSet" => string.Join(",", entry.SortedSetValue?.Select(kv => $"{kv.Key}:{kv.Value}") ?? []),
			"Stream" => string.Join(";", entry.StreamValue?.Select(e => $"{e.Id}:[{string.Join(",", e.Fields.Select(f => $"{f.Key}={f.Value}"))}]") ?? []),
			_ => string.Empty
		};

		TimeSpan? ttl = entry.ExpiresAt.HasValue ? entry.ExpiresAt.Value - DateTime.UtcNow : null;
		if (ttl.HasValue && ttl.Value <= TimeSpan.Zero)
		{
			store.TryRemove(key, out _);
			return Task.FromResult(Result.Failure<RedisValue>(DomainErrors.Redis.KeyNotFound));
		}

		Result<RedisValue> result = RedisValue.FromRedis(key, value, entry.Type, ttl);
		return Task.FromResult(result);
	}

	public Task<Result<PagedRedisValues>> GetAllValues(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default)
	{
		// Clean expired keys first
		CleanExpiredKeys();

		List<RedisValue> items = store
			.Where(kvp => !IsExpired(kvp.Value))
			.Select(kvp =>
			{
				string value = kvp.Value.Type switch
				{
					"String" => kvp.Value.StringValue ?? string.Empty,
					"List" => string.Join(",", kvp.Value.ListValue ?? []),
					"Set" => string.Join(",", kvp.Value.SetValue ?? []),
					"Hash" => string.Join(",", kvp.Value.HashValue?.Select(h => $"{h.Key}:{h.Value}") ?? []),
					"SortedSet" => string.Join(",", kvp.Value.SortedSetValue?.Select(s => $"{s.Key}:{s.Value}") ?? []),
					"Stream" => string.Join(";", kvp.Value.StreamValue?.Select(e => $"{e.Id}:[{string.Join(",", e.Fields.Select(f => $"{f.Key}={f.Value}"))}]") ?? []),
					_ => string.Empty
				};

				TimeSpan? ttl = kvp.Value.ExpiresAt.HasValue ? kvp.Value.ExpiresAt.Value - DateTime.UtcNow : null;
				return RedisValue.FromRedis(kvp.Key, value, kvp.Value.Type, ttl).Value;
			})
			.ToList();

		List<RedisValue> filtered = ApplyPattern(items, pattern);
		int total = filtered.Count;
		int size = pageSize ?? total;
		int page = pageNumber ?? 1;
		List<RedisValue> pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
		PagedRedisValues paged = PagedRedisValues.Create(pageItems, total, page, size);
		return Task.FromResult(Result.Success(paged));
	}

	public Task<Result<bool>> DeleteKey(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		bool removed = store.TryRemove(key, out _);
		return Task.FromResult(Result.Success(removed));
	}

	public Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeys(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default)
	{
		List<string> failed = [];
		int deleted = 0;
		foreach (string key in keys)
		{
			if (store.TryRemove(key, out _))
			{
				deleted++;
			}
			else
			{
				failed.Add(key);
			}
		}
		return Task.FromResult(Result.Success((deleted, (IReadOnlyList<string>)failed)));
	}

	public Task<Result<int>> DeleteKeysByPattern(RedisConnection connection, string pattern, CancellationToken cancellationToken = default)
	{
		List<string> filtered = ApplyPattern(store.Keys.ToList(), pattern);
		int count = 0;
		foreach (string key in filtered)
		{
			if (store.TryRemove(key, out _))
			{
				count++;
			}
		}
		return Task.FromResult(Result.Success(count));
	}

	public Task<Result<string>> GetKeyType(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		if (!store.TryGetValue(key, out MockRedisValue? entry) || IsExpired(entry))
		{
			return Task.FromResult(Result.Failure<string>(DomainErrors.Redis.KeyNotFound));
		}
		return Task.FromResult(Result.Success(entry.Type));
	}

	public Task<Result> UpdateValue(RedisConnection connection, string key, object value, string type, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
	{
		try
		{
			MockRedisValue mockValue = new()
			{
				Type = type,
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
			};

			switch (type.ToUpperInvariant())
			{
				case "STRING":
					mockValue.StringValue = value?.ToString() ?? string.Empty;
					break;

				case "LIST":
					if (value is IEnumerable<string> listItems)
					{
						mockValue.ListValue = listItems.ToList();
					}
					else
					{
						return Task.FromResult(Result.Failure(DomainErrors.Redis.InvalidValue));
					}
					break;

				case "SET":
					if (value is IEnumerable<string> setMembers)
					{
						mockValue.SetValue = setMembers.ToHashSet();
					}
					else
					{
						return Task.FromResult(Result.Failure(DomainErrors.Redis.InvalidValue));
					}
					break;

				case "HASH":
					if (value is Dictionary<string, string> hashFields)
					{
						mockValue.HashValue = hashFields;
					}
					else
					{
						return Task.FromResult(Result.Failure(DomainErrors.Redis.InvalidValue));
					}
					break;

				case "SORTEDSET":
					if (value is Dictionary<string, double> sortedSetMembers)
					{
						mockValue.SortedSetValue = sortedSetMembers;
					}
					else
					{
						return Task.FromResult(Result.Failure(DomainErrors.Redis.InvalidValue));
					}
					break;

				case "STREAM":
					if (value is List<Dictionary<string, string>> streamEntries)
					{
						mockValue.StreamValue = streamEntries.Select((entry, index) => new MockStreamEntry
						{
							Id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{index}",
							Fields = entry
						}).ToList();
					}
					else
					{
						return Task.FromResult(Result.Failure(DomainErrors.Redis.InvalidValue));
					}
					break;

				default:
					return Task.FromResult(Result.Failure(DomainErrors.Redis.UnsupportedType));
			}

			store[key] = mockValue;
			return Task.FromResult(Result.Success());
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error updating mock Redis value for key {Key}", key);
			return Task.FromResult(Result.Failure(DomainErrors.Redis.UpdateFailed));
		}
	}

	public async Task<Result> PublishMessage(RedisConnection connection, string channel, string message, CancellationToken cancellationToken = default)
	{
		try
		{
			// Store message in channel
			if (!pubSubChannels.ContainsKey(channel))
			{
				pubSubChannels[channel] = [];
			}
			pubSubChannels[channel].Add(message);

			// Broadcast to all subscribers via SignalR
			int subscribersCount = 0;
			foreach (var subscription in subscriptions.Values.Where(s => s.Channel == channel))
			{
				subscribersCount++;
				if (pubSubNotificationService != null)
				{
					PubSubMessageDto messageDto = new(
						ConnectionId: subscription.ConnectionId.ToString(),
						Channel: channel,
						Message: message,
						Timestamp: DateTime.UtcNow.ToString("O"));

					try
					{
						await pubSubNotificationService.BroadcastMessage(subscription.SubscriptionId, messageDto, cancellationToken);
					}
					catch (Exception ex)
					{
						logger?.LogError(ex, "Failed to broadcast Pub/Sub message via SignalR for subscription {SubscriptionId}", subscription.SubscriptionId);
					}
				}
			}

			logger?.LogInformation("Published message to channel {Channel}, received by {Recipients} subscribers", channel, subscribersCount);
			return Result.Success();
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error publishing message to channel {Channel}", channel);
			return Result.Failure(DomainErrors.Redis.PublishFailed);
		}
	}

	public Task<Result<string>> SubscribeToChannel(
		RedisConnection connection,
		string channel,
		CancellationToken cancellationToken)
	{
		try
		{
			PubSubSubscription subscription = PubSubSubscription.Create(connection.Id, channel);
			subscriptions.TryAdd(subscription.SubscriptionId, subscription);

			if (!pubSubChannels.ContainsKey(channel))
			{
				pubSubChannels[channel] = [];
			}

			logger?.LogInformation("Subscribed to channel {Channel} with ID {SubscriptionId}", channel, subscription.SubscriptionId);
			return Task.FromResult(Result.Success(subscription.SubscriptionId));
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error subscribing to channel {Channel}", channel);
			return Task.FromResult(Result.Failure<string>(DomainErrors.Redis.SubscribeFailed));
		}
	}

	public Task<Result> UnsubscribeFromChannel(
		RedisConnection connection,
		string subscriptionId,
		CancellationToken cancellationToken)
	{
		try
		{
			if (!subscriptions.TryRemove(subscriptionId, out PubSubSubscription? subscription))
			{
				return Task.FromResult(Result.Failure(DomainErrors.Redis.SubscriptionNotFound));
			}

			logger?.LogInformation("Unsubscribed from channel {Channel}", subscription.Channel);
			return Task.FromResult(Result.Success());
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error unsubscribing from subscription {SubscriptionId}", subscriptionId);
			return Task.FromResult(Result.Failure(DomainErrors.Redis.UnsubscribeFailed));
		}
	}

	public Task<Result<List<string>>> ExecuteTransaction(
		RedisConnection connection,
		List<RedisOperation> operations,
		CancellationToken cancellationToken = default)
	{
		try
		{
			List<string> results = [];
			foreach (RedisOperation op in operations ?? [])
			{
				string result = op.Command.ToUpperInvariant() switch
				{
					"SET" => ExecuteSet(op),
					"GET" => ExecuteGet(op),
					"DEL" => ExecuteDel(op),
					"INCR" => ExecuteIncr(op),
					"DECR" => ExecuteDecr(op),
					"LPUSH" => ExecuteLPush(op),
					"RPUSH" => ExecuteRPush(op),
					"LPOP" => ExecuteLPop(op),
					"RPOP" => ExecuteRPop(op),
					"HSET" => ExecuteHSet(op),
					"HGET" => ExecuteHGet(op),
					"SADD" => ExecuteSAdd(op),
					"SMEMBERS" => ExecuteSMembers(op),
					"ZADD" => ExecuteZAdd(op),
					"ZRANGE" => ExecuteZRange(op),
					_ => $"ERR: Unknown command {op.Command}"
				};
				results.Add(result);
			}
			return Task.FromResult(Result.Success(results));
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error executing transaction");
			return Task.FromResult(Result.Failure<List<string>>(DomainErrors.Redis.TransactionFailed));
		}
	}

	public Task<Result<(string Result, string ScriptHash)>> ExecuteLuaScript(
		RedisConnection connection,
		string script,
		string[] keys,
		string[] arguments,
		CancellationToken cancellationToken = default)
	{
		try
		{
			// Generate SHA1 hash for the script
			string scriptHash = ComputeSha1(script);

			// Basic Lua script execution - parse and execute simple commands
			string result = ExecuteLuaScriptContent(script, keys, arguments);

			return Task.FromResult(Result.Success((result, scriptHash)));
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error executing Lua script");
			return Task.FromResult(Result.Failure<(string, string)>(DomainErrors.Redis.ScriptFailed));
		}
	}

	private string ExecuteSet(RedisOperation op)
	{
		if (string.IsNullOrEmpty(op.Value))
		{
			return "ERR: SET requires a value";
		}
		store[op.Key] = new MockRedisValue
		{
			Type = "String",
			StringValue = op.Value,
			CreatedAt = DateTime.UtcNow
		};
		return "OK";
	}

	private string ExecuteGet(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value))
		{
			return value.StringValue ?? string.Empty;
		}
		return "(nil)";
	}

	private string ExecuteDel(RedisOperation op)
	{
		return store.TryRemove(op.Key, out _) ? "1" : "0";
	}

	private string ExecuteIncr(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value))
		{
			if (long.TryParse(value.StringValue, out long current))
			{
				long newValue = current + 1;
				value.StringValue = newValue.ToString();
				return newValue.ToString();
			}
		}
		else
		{
			store[op.Key] = new MockRedisValue
			{
				Type = "String",
				StringValue = "1",
				CreatedAt = DateTime.UtcNow
			};
			return "1";
		}
		return "ERR: value is not an integer";
	}

	private string ExecuteDecr(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value))
		{
			if (long.TryParse(value.StringValue, out long current))
			{
				long newValue = current - 1;
				value.StringValue = newValue.ToString();
				return newValue.ToString();
			}
		}
		else
		{
			store[op.Key] = new MockRedisValue
			{
				Type = "String",
				StringValue = "-1",
				CreatedAt = DateTime.UtcNow
			};
			return "-1";
		}
		return "ERR: value is not an integer";
	}

	private string ExecuteLPush(RedisOperation op)
	{
		if (!store.TryGetValue(op.Key, out MockRedisValue? value) || IsExpired(value))
		{
			value = new MockRedisValue { Type = "List", ListValue = [], CreatedAt = DateTime.UtcNow };
			store[op.Key] = value;
		}
		value.ListValue ??= [];
		value.ListValue.Insert(0, op.Value ?? string.Empty);
		return value.ListValue.Count.ToString();
	}

	private string ExecuteRPush(RedisOperation op)
	{
		if (!store.TryGetValue(op.Key, out MockRedisValue? value) || IsExpired(value))
		{
			value = new MockRedisValue { Type = "List", ListValue = [], CreatedAt = DateTime.UtcNow };
			store[op.Key] = value;
		}
		value.ListValue ??= [];
		value.ListValue.Add(op.Value ?? string.Empty);
		return value.ListValue.Count.ToString();
	}

	private string ExecuteLPop(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value) && value.ListValue?.Count > 0)
		{
			string item = value.ListValue[0];
			value.ListValue.RemoveAt(0);
			return item;
		}
		return "(nil)";
	}

	private string ExecuteRPop(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value) && value.ListValue?.Count > 0)
		{
			string item = value.ListValue[^1];
			value.ListValue.RemoveAt(value.ListValue.Count - 1);
			return item;
		}
		return "(nil)";
	}

	private string ExecuteHSet(RedisOperation op)
	{
		if (!store.TryGetValue(op.Key, out MockRedisValue? value) || IsExpired(value))
		{
			value = new MockRedisValue { Type = "Hash", HashValue = [], CreatedAt = DateTime.UtcNow };
			store[op.Key] = value;
		}
		value.HashValue ??= [];
		if (op.Arguments?.Length >= 1)
		{
			string field = op.Arguments[0];
			string fieldValue = op.Value ?? string.Empty;
			value.HashValue[field] = fieldValue;
			return "1";
		}
		return "0";
	}

	private string ExecuteHGet(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value) && value.HashValue != null)
		{
			if (op.Arguments?.Length >= 1 && value.HashValue.TryGetValue(op.Arguments[0], out string? fieldValue))
			{
				return fieldValue;
			}
		}
		return "(nil)";
	}

	private string ExecuteSAdd(RedisOperation op)
	{
		if (!store.TryGetValue(op.Key, out MockRedisValue? value) || IsExpired(value))
		{
			value = new MockRedisValue { Type = "Set", SetValue = [], CreatedAt = DateTime.UtcNow };
			store[op.Key] = value;
		}
		value.SetValue ??= [];
		bool added = value.SetValue.Add(op.Value ?? string.Empty);
		return added ? "1" : "0";
	}

	private string ExecuteSMembers(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value) && value.SetValue != null)
		{
			return string.Join(",", value.SetValue);
		}
		return "(empty list or set)";
	}

	private string ExecuteZAdd(RedisOperation op)
	{
		if (!store.TryGetValue(op.Key, out MockRedisValue? value) || IsExpired(value))
		{
			value = new MockRedisValue { Type = "SortedSet", SortedSetValue = [], CreatedAt = DateTime.UtcNow };
			store[op.Key] = value;
		}
		value.SortedSetValue ??= [];
		if (op.Arguments?.Length >= 1 && double.TryParse(op.Arguments[0], out double score))
		{
			value.SortedSetValue[op.Value ?? string.Empty] = score;
			return "1";
		}
		return "0";
	}

	private string ExecuteZRange(RedisOperation op)
	{
		if (store.TryGetValue(op.Key, out MockRedisValue? value) && !IsExpired(value) && value.SortedSetValue != null)
		{
			var sorted = value.SortedSetValue.OrderBy(kv => kv.Value).Select(kv => kv.Key);
			return string.Join(",", sorted);
		}
		return "(empty list or set)";
	}

	private string ExecuteLuaScriptContent(string script, string[] keys, string[] arguments)
	{
		// Basic Lua script interpreter for common patterns
		// This is a simplified implementation that handles basic Redis commands in Lua

		// Replace KEYS[] and ARGV[] with actual values
		string processedScript = script;
		for (int i = 0; i < keys.Length; i++)
		{
			processedScript = processedScript.Replace($"KEYS[{i + 1}]", keys[i]);
		}
		for (int i = 0; i < arguments.Length; i++)
		{
			processedScript = processedScript.Replace($"ARGV[{i + 1}]", arguments[i]);
		}

		// Simple pattern matching for common Redis commands
		if (processedScript.Contains("redis.call"))
		{
			// Extract redis.call commands
		MatchCollection matches = RedisCallRegex().Matches(processedScript);
		foreach (Match match in matches)
		{
			string command = match.Groups[1].Value;
			string key = match.Groups[2].Value;

			if (command == "GET")
			{
				if (store.TryGetValue(key, out MockRedisValue? value) && !IsExpired(value))
				{
					processedScript = processedScript.Replace(match.Value, $"'{value.StringValue}'");
				}
				else
				{
					processedScript = processedScript.Replace(match.Value, "nil");
				}
			}
			else if (command == "SET")
			{
				// Extract value from script
				Match valueMatch = SetValueRegex().Match(processedScript);
			if (valueMatch.Success)
			{
				ExecuteSet(new RedisOperation("SET", key, valueMatch.Groups[1].Value));
				processedScript = processedScript.Replace(match.Value, "'OK'");
			}
		}
	}
}

// For now, return a mock result
// In a production mock, you'd want a more sophisticated Lua interpreter
return "mock-lua-result";
}

private static string ComputeSha1(string input)
{
	byte[] hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(input));
	return Convert.ToHexString(hash).ToLowerInvariant();
}

private static List<T> ApplyPattern<T>(IEnumerable<T> items, string pattern)
{
	if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
	{
		return items.ToList();
	}

	// Convert Redis pattern to regex
	string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
	Regex regex = new(regexPattern, RegexOptions.IgnoreCase);
	return items.Where(i => regex.IsMatch(i?.ToString() ?? string.Empty)).ToList();
}

	private static bool IsExpired(MockRedisValue value)
	{
		return value.ExpiresAt.HasValue && value.ExpiresAt.Value <= DateTime.UtcNow;
	}

	private void CleanExpiredKeys()
	{
		List<string> expiredKeys = store
			.Where(kvp => IsExpired(kvp.Value))
			.Select(kvp => kvp.Key)
			.ToList();

		foreach (string key in expiredKeys)
		{
			store.TryRemove(key, out _);
		}
	}

	private void ExpireKeysWithTtl(object? state)
	{
		CleanExpiredKeys();
	}

	private void SeedDummyData()
	{
		Random random = new();
		int keyCount = options.DummyDataKeyCount;

		// Generate nested key with random depth (2-5 segments) and random segment names
		string GenerateNestedKey(string prefix, int index)
		{
			int depth = random.Next(2, 6); // 2 to 5 segments (excluding prefix)
			List<string> segments = [prefix];
			
			// Common segment name patterns for more realistic keys
			string[] segmentPatterns = ["subkey", "item", "data", "entry", "node", "path", "segment", "part", "level", "branch"];
			
			for (int d = 1; d < depth; d++)
			{
				// Random segment name with variety
				string pattern = segmentPatterns[random.Next(segmentPatterns.Length)];
				int segmentNumber = random.Next(1, 1000);
				string segment = $"{pattern}{segmentNumber}";
				segments.Add(segment);
			}
			
			// Add index at the end (sometimes, for variety)
			if (random.Next(0, 2) == 0)
			{
				segments.Add(index.ToString());
			}
			
			return string.Join(":", segments);
		}

		// Seed String keys with nested structure
		for (int i = 0; i < keyCount / 6; i++)
		{
			store[GenerateNestedKey("string", i)] = new MockRedisValue
			{
				Type = "String",
				StringValue = $"value-{i}-{Guid.NewGuid()}",
				CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
			};
		}

		// Seed List keys with nested structure
		for (int i = 0; i < keyCount / 6; i++)
		{
			store[GenerateNestedKey("list", i)] = new MockRedisValue
			{
				Type = "List",
				ListValue = Enumerable.Range(0, random.Next(3, 10)).Select(j => $"item-{j}").ToList(),
				CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
			};
		}

		// Seed Set keys with nested structure
		for (int i = 0; i < keyCount / 6; i++)
		{
			store[GenerateNestedKey("set", i)] = new MockRedisValue
			{
				Type = "Set",
				SetValue = Enumerable.Range(0, random.Next(3, 10)).Select(j => $"member-{j}").ToHashSet(),
				CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
			};
		}

		// Seed Hash keys with nested structure
		for (int i = 0; i < keyCount / 6; i++)
		{
			store[GenerateNestedKey("hash", i)] = new MockRedisValue
			{
				Type = "Hash",
				HashValue = Enumerable.Range(0, random.Next(3, 10))
					.ToDictionary(j => $"field-{j}", j => $"value-{j}"),
				CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
			};
		}

		// Seed SortedSet keys with nested structure
		for (int i = 0; i < keyCount / 6; i++)
		{
			store[GenerateNestedKey("sortedset", i)] = new MockRedisValue
			{
				Type = "SortedSet",
				SortedSetValue = Enumerable.Range(0, random.Next(3, 10))
					.ToDictionary(j => $"member-{j}", j => (double)random.Next(1, 100)),
				CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
			};
		}

		// Seed Stream keys with nested structure
		for (int i = 0; i < keyCount / 6; i++)
		{
			store[GenerateNestedKey("stream", i)] = new MockRedisValue
			{
				Type = "Stream",
				StreamValue = Enumerable.Range(0, random.Next(3, 10))
					.Select(j => new MockStreamEntry
					{
						Id = $"{DateTimeOffset.UtcNow.AddSeconds(-random.Next(0, 3600)).ToUnixTimeMilliseconds()}-{j}",
						Fields = new Dictionary<string, string> { ["field1"] = $"value-{j}", ["field2"] = $"data-{j}" }
					})
					.ToList(),
				CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
			};
		}

		// Seed some keys with TTL and nested structure
		for (int i = 0; i < keyCount / 10; i++)
		{
			store[GenerateNestedKey("ttl", i)] = new MockRedisValue
			{
				Type = "String",
				StringValue = $"ttl-value-{i}",
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddSeconds(random.Next(60, 3600))
			};
		}

		logger?.LogInformation("Seeded {Count} dummy Redis keys with nested structure", store.Count);
	}

	/// <summary>
	/// Internal class to represent a mock Redis value with all data types
	/// </summary>
	private sealed class MockRedisValue
	{
		public string Type { get; set; } = "String";
		public string? StringValue { get; set; }
		public List<string>? ListValue { get; set; }
		public HashSet<string>? SetValue { get; set; }
		public Dictionary<string, string>? HashValue { get; set; }
		public Dictionary<string, double>? SortedSetValue { get; set; }
		public List<MockStreamEntry>? StreamValue { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? ExpiresAt { get; set; }
	}

	/// <summary>
	/// Internal class to represent a stream entry
	/// </summary>
	private sealed class MockStreamEntry
	{
		public string Id { get; set; } = string.Empty;
		public Dictionary<string, string> Fields { get; set; } = [];
	}
}
