using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.Mock;
using RedisGUI.Shared.Primitives;
using System.Linq;
using System.Text.RegularExpressions;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Mock implementation of IRedisValueService for testing
/// </summary>
public sealed class MockRedisValueService : IRedisValueService
{
	private readonly MockRedisStore _store;
	private readonly ILogger<MockRedisValueService>? _logger;

	public MockRedisValueService(MockRedisStore store, ILogger<MockRedisValueService>? logger = null)
	{
		_store = store;
		_logger = logger;
	}

	public Task<Result<RedisValue>> GetValueAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		if (!_store.TryGetValue(key, out var entry) || entry == null || _store.IsExpired(entry))
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
			_store.TryRemove(key, out _);
			return Task.FromResult(Result.Failure<RedisValue>(DomainErrors.Redis.KeyNotFound));
		}

		Result<RedisValue> result = RedisValue.FromRedis(key, value, entry.Type, ttl);
		_logger?.LogDebug("Retrieved value for key {Key} of type {Type}", key, entry.Type);
		return Task.FromResult(result);
	}

	public Task<Result> SetValueAsync(RedisConnection connection, string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
	{
		_store[key] = new MockRedisValue
		{
			Type = "String",
			StringValue = value,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
		};
		_logger?.LogDebug("Set value for key {Key} with TTL {TTL}", key, ttl);
		return Task.FromResult(Result.Success());
	}

	public Task<Result> UpdateValueAsync(RedisConnection connection, string key, object value, string type, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
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

			_store[key] = mockValue;
			_logger?.LogDebug("Updated value for key {Key} of type {Type}", key, type);
			return Task.FromResult(Result.Success());
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error updating mock Redis value for key {Key}", key);
			return Task.FromResult(Result.Failure(DomainErrors.Redis.UpdateFailed));
		}
	}

	public Task<Result<PagedRedisValues>> GetAllValuesAsync(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default)
	{
		List<RedisValue> items = _store.Keys
			.Where(key =>
			{
				var value = _store[key];
				return value != null && !_store.IsExpired(value);
			})
			.Select(key =>
			{
				var kvp = _store[key]!;
				string value = kvp.Type switch
				{
					"String" => kvp.StringValue ?? string.Empty,
					"List" => string.Join(",", kvp.ListValue ?? []),
					"Set" => string.Join(",", kvp.SetValue ?? []),
					"Hash" => string.Join(",", kvp.HashValue?.Select(h => $"{h.Key}:{h.Value}") ?? []),
					"SortedSet" => string.Join(",", kvp.SortedSetValue?.Select(s => $"{s.Key}:{s.Value}") ?? []),
					"Stream" => string.Join(";", kvp.StreamValue?.Select(e => $"{e.Id}:[{string.Join(",", e.Fields.Select(f => $"{f.Key}={f.Value}"))}]") ?? []),
					_ => string.Empty
				};

				TimeSpan? ttl = kvp.ExpiresAt.HasValue ? kvp.ExpiresAt.Value - DateTime.UtcNow : null;
				return RedisValue.FromRedis(key, value, kvp.Type, ttl).Value;
			})
			.ToList();

		List<RedisValue> filtered = ApplyPattern(items, pattern);
		int total = filtered.Count;
		int size = pageSize ?? total;
		int page = pageNumber ?? 1;
		List<RedisValue> pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();
		PagedRedisValues paged = PagedRedisValues.Create(pageItems, total, page, size);
		
		_logger?.LogDebug("Retrieved {Count} values matching pattern {Pattern} (page {Page} of size {Size})", 
			pageItems.Count, pattern, page, size);
		
		return Task.FromResult(Result.Success(paged));
	}

	/// <summary>
	/// Applies a Redis pattern to filter items
	/// </summary>
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
}
