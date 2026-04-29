using System.Collections.Concurrent;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Mock;

/// <summary>
/// Shared in-memory storage for all mock Redis services
/// </summary>
public sealed class MockRedisStore
{
	private readonly ConcurrentDictionary<string, MockRedisValue> store = new();
	private readonly Timer cleanupTimer;
	private readonly ILogger<MockRedisStore>? logger;

	public MockRedisStore(ILogger<MockRedisStore>? logger = null)
	{
		this.logger = logger;
		cleanupTimer = new Timer(CleanExpiredKeys, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
	}

	/// <summary>
	/// Gets or sets a value in the store
	/// </summary>
	public MockRedisValue? this[string key]
	{
		get => store.TryGetValue(key, out var value) ? value : null;
		set
		{
			if (value != null)
			{
				store[key] = value;
			}
		}
	}

	/// <summary>
	/// Gets all keys in the store
	/// </summary>
	public IEnumerable<string> Keys => store.Keys;

	/// <summary>
	/// Attempts to get a value from the store
	/// </summary>
	public bool TryGetValue(string key, out MockRedisValue? value) =>
		store.TryGetValue(key, out value);

	/// <summary>
	/// Attempts to remove a value from the store
	/// </summary>
	public bool TryRemove(string key, out MockRedisValue? value) =>
		store.TryRemove(key, out value);

	/// <summary>
	/// Checks if a value has expired
	/// </summary>
	public bool IsExpired(MockRedisValue value) =>
		value.ExpiresAt.HasValue && value.ExpiresAt.Value <= DateTime.UtcNow;

	/// <summary>
	/// Cleans up expired keys from the store
	/// </summary>
	private void CleanExpiredKeys(object? state = null)
	{
		List<string> expiredKeys = store
			.Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= DateTime.UtcNow)
			.Select(kvp => kvp.Key)
			.ToList();

		foreach (string key in expiredKeys)
		{
			store.TryRemove(key, out _);
		}

		if (expiredKeys.Count > 0)
		{
			logger?.LogDebug("Cleaned up {Count} expired keys from mock Redis store", expiredKeys.Count);
		}
	}

	/// <summary>
	/// Seeds the store with dummy data for testing
	/// </summary>
	public void SeedData(int keyCount = 1000)
	{
		Random random = new();

		for (int i = 0; i < keyCount; i++)
		{
			string keyName = i switch
			{
				_ when i % 5 == 0 => $"user:profile:{i}",
				_ when i % 5 == 1 => $"session:token:{i}",
				_ when i % 5 == 2 => $"cache:data:{i}",
				_ when i % 5 == 3 => $"config:setting:{i}",
				_ => $"key:{i}"
			};

			string typeChoice = i switch
			{
				_ when i % 6 == 0 => "List",
				_ when i % 6 == 1 => "Set",
				_ when i % 6 == 2 => "Hash",
				_ when i % 6 == 3 => "SortedSet",
				_ when i % 6 == 4 => "Stream",
				_ => "String"
			};

			store[keyName] = typeChoice switch
			{
				"String" => new MockRedisValue
				{
					Type = "String",
					StringValue = $"value-{i}",
					CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
				},
				"List" => new MockRedisValue
				{
					Type = "List",
					ListValue = Enumerable.Range(0, random.Next(5, 15))
						.Select(j => $"item-{j}")
						.ToList(),
					CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
				},
				"Set" => new MockRedisValue
				{
					Type = "Set",
					SetValue = Enumerable.Range(0, random.Next(5, 15))
						.Select(j => $"member-{j}")
						.ToHashSet(),
					CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
				},
				"Hash" => new MockRedisValue
				{
					Type = "Hash",
					HashValue = Enumerable.Range(0, random.Next(5, 15))
						.ToDictionary(j => $"field-{j}", j => $"value-{j}"),
					CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
				},
				"SortedSet" => new MockRedisValue
				{
					Type = "SortedSet",
					SortedSetValue = Enumerable.Range(0, random.Next(5, 15))
						.ToDictionary(j => $"member-{j}", j => (double)j),
					CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
				},
				"Stream" => new MockRedisValue
				{
					Type = "Stream",
					StreamValue = Enumerable.Range(0, random.Next(5, 15))
						.Select(j => new MockStreamEntry
						{
							Id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{j}",
							Fields = new Dictionary<string, string> { ["field1"] = $"value-{j}", ["field2"] = $"data-{j}" }
						})
						.ToList(),
					CreatedAt = DateTime.UtcNow.AddSeconds(-random.Next(0, 3600))
				},
				_ => new MockRedisValue
				{
					Type = "String",
					StringValue = $"value-{i}",
					CreatedAt = DateTime.UtcNow
				}
			};
		}

		// Add keys with TTL
		for (int i = 0; i < keyCount / 10; i++)
		{
			store[$"ttl:key:{i}"] = new MockRedisValue
			{
				Type = "String",
				StringValue = $"ttl-value-{i}",
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddSeconds(random.Next(60, 3600))
			};
		}

		logger?.LogInformation("Seeded {Count} dummy Redis keys with various data types", store.Count);
	}

	/// <summary>
	/// Clears all data from the store
	/// </summary>
	public void Clear() => store.Clear();

	/// <summary>
	/// Gets the count of items in the store
	/// </summary>
	public int Count => store.Count;
}
