using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.Mock;
using RedisGUI.Shared.Primitives;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Mock implementation of IRedisTransactionService for testing
/// </summary>
public sealed class MockRedisTransactionService : IRedisTransactionService
{
	private readonly MockRedisStore _store;
	private readonly ILogger<MockRedisTransactionService>? _logger;

	public MockRedisTransactionService(MockRedisStore store, ILogger<MockRedisTransactionService>? logger = null)
	{
		_store = store;
		_logger = logger;
	}

	public Task<Result<List<string>>> ExecuteTransactionAsync(
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
			_logger?.LogDebug("Executed transaction with {Count} operations", operations?.Count ?? 0);
			return Task.FromResult(Result.Success(results));
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error executing transaction");
			return Task.FromResult(Result.Failure<List<string>>(DomainErrors.Redis.TransactionFailed));
		}
	}

	private string ExecuteSet(RedisOperation op)
	{
		if (string.IsNullOrEmpty(op.Value))
		{
			return "ERR: SET requires a value";
		}
		_store[op.Key] = new MockRedisValue
		{
			Type = "String",
			StringValue = op.Value,
			CreatedAt = DateTime.UtcNow
		};
		return "OK";
	}

	private string ExecuteGet(RedisOperation op)
	{
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value))
		{
			return value.StringValue ?? string.Empty;
		}
		return "(nil)";
	}

	private string ExecuteDel(RedisOperation op)
	{
		return _store.TryRemove(op.Key, out _) ? "1" : "0";
	}

	private string ExecuteIncr(RedisOperation op)
	{
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value))
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
			_store[op.Key] = new MockRedisValue
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
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value))
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
			_store[op.Key] = new MockRedisValue
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
		if (!_store.TryGetValue(op.Key, out var value) || value == null || _store.IsExpired(value))
		{
			value = new MockRedisValue { Type = "List", ListValue = [], CreatedAt = DateTime.UtcNow };
			_store[op.Key] = value;
		}
		value.ListValue ??= [];
		value.ListValue.Insert(0, op.Value ?? string.Empty);
		return value.ListValue.Count.ToString();
	}

	private string ExecuteRPush(RedisOperation op)
	{
		if (!_store.TryGetValue(op.Key, out var value) || value == null || _store.IsExpired(value))
		{
			value = new MockRedisValue { Type = "List", ListValue = [], CreatedAt = DateTime.UtcNow };
			_store[op.Key] = value;
		}
		value.ListValue ??= [];
		value.ListValue.Add(op.Value ?? string.Empty);
		return value.ListValue.Count.ToString();
	}

	private string ExecuteLPop(RedisOperation op)
	{
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value) && value.ListValue?.Count > 0)
		{
			string item = value.ListValue[0];
			value.ListValue.RemoveAt(0);
			return item;
		}
		return "(nil)";
	}

	private string ExecuteRPop(RedisOperation op)
	{
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value) && value.ListValue?.Count > 0)
		{
			string item = value.ListValue[^1];
			value.ListValue.RemoveAt(value.ListValue.Count - 1);
			return item;
		}
		return "(nil)";
	}

	private string ExecuteHSet(RedisOperation op)
	{
		if (!_store.TryGetValue(op.Key, out var value) || value == null || _store.IsExpired(value))
		{
			value = new MockRedisValue { Type = "Hash", HashValue = [], CreatedAt = DateTime.UtcNow };
			_store[op.Key] = value;
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
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value) && value.HashValue != null)
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
		if (!_store.TryGetValue(op.Key, out var value) || value == null || _store.IsExpired(value))
		{
			value = new MockRedisValue { Type = "Set", SetValue = [], CreatedAt = DateTime.UtcNow };
			_store[op.Key] = value;
		}
		value.SetValue ??= [];
		bool added = value.SetValue.Add(op.Value ?? string.Empty);
		return added ? "1" : "0";
	}

	private string ExecuteSMembers(RedisOperation op)
	{
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value) && value.SetValue != null)
		{
			return string.Join(",", value.SetValue);
		}
		return "(empty list or set)";
	}

	private string ExecuteZAdd(RedisOperation op)
	{
		if (!_store.TryGetValue(op.Key, out var value) || value == null || _store.IsExpired(value))
		{
			value = new MockRedisValue { Type = "SortedSet", SortedSetValue = [], CreatedAt = DateTime.UtcNow };
			_store[op.Key] = value;
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
		if (_store.TryGetValue(op.Key, out var value) && value != null && !_store.IsExpired(value) && value.SortedSetValue != null)
		{
			var sorted = value.SortedSetValue.OrderBy(kv => kv.Value).Select(kv => kv.Key);
			return string.Join(",", sorted);
		}
		return "(empty list or set)";
	}
}
