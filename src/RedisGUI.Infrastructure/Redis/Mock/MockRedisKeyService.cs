using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Infrastructure.Redis.Mock;
using RedisGUI.Shared.Primitives;
using System.Linq;
using System.Text.RegularExpressions;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Mock implementation of IRedisKeyService for testing
/// </summary>
public sealed partial class MockRedisKeyService : IRedisKeyService
{
	private readonly MockRedisStore _store;
	private readonly ILogger<MockRedisKeyService>? _logger;

	public MockRedisKeyService(MockRedisStore store, ILogger<MockRedisKeyService>? logger = null)
	{
		_store = store;
		_logger = logger;
	}

	public ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		IEnumerable<string> keys = _store.Keys.Where(key =>
		{
			var value = _store[key];
			return value != null && !_store.IsExpired(value);
		});
		return ValueTask.FromResult(Result.Success(keys));
	}

	public Task<Result<bool>> DeleteKeyAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		bool removed = _store.TryRemove(key, out _);
		_logger?.LogDebug("Deleted key {Key}: {Success}", key, removed);
		return Task.FromResult(Result.Success(removed));
	}

	public Task<Result<long>> DeleteKeysAsync(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default)
	{
		long deleted = 0;
		foreach (string key in keys)
		{
			if (_store.TryRemove(key, out _))
			{
				deleted++;
			}
		}
		_logger?.LogDebug("Deleted {Count} out of {Total} keys", deleted, keys.Length);
		return Task.FromResult(Result.Success(deleted));
	}

	public Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default)
	{
		List<string> failed = [];
		int deleted = 0;
		foreach (string key in keys)
		{
			if (_store.TryRemove(key, out _))
			{
				deleted++;
			}
			else
			{
				failed.Add(key);
			}
		}
		_logger?.LogDebug("Deleted {Deleted} keys, {Failed} failed", deleted, failed.Count);
		return Task.FromResult(Result.Success((deleted, (IReadOnlyList<string>)failed)));
	}

	public Task<Result<int>> DeleteKeysByPatternAsync(RedisConnection connection, string pattern, CancellationToken cancellationToken = default)
	{
		List<string> matchingKeys = ApplyPattern(_store.Keys.ToList(), pattern);
		int count = 0;
		foreach (string key in matchingKeys)
		{
			if (_store.TryRemove(key, out _))
			{
				count++;
			}
		}
		_logger?.LogDebug("Deleted {Count} keys matching pattern {Pattern}", count, pattern);
		return Task.FromResult(Result.Success(count));
	}

	public Task<Result<string>> GetKeyTypeAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		if (!_store.TryGetValue(key, out var entry) || entry == null || _store.IsExpired(entry))
		{
			return Task.FromResult(Result.Failure<string>(DomainErrors.Redis.KeyNotFound));
		}
		return Task.FromResult(Result.Success(entry.Type));
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
