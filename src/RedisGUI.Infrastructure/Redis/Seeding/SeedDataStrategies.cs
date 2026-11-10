using StackExchange.Redis;
using System.Text.Json;

namespace RedisGUI.Infrastructure.Redis.Seeding;

/// <summary>
/// Seeding strategy for Redis Strings.
/// </summary>
public sealed class StringSeedStrategy : ISeedDataStrategy
{
	public string DataType => "string";

	public async Task SeedAsync(IDatabase db, string key, string payloadComplexity, int index, TimeSpan? ttl, Random rng)
	{
		string strVal = (payloadComplexity ?? "json").ToLowerInvariant() switch
		{
			"simple" => $"value_{index + 1}",
			"complex" => JsonSerializer.Serialize(new
			{
				id = 10000 + index,
				guid = Guid.NewGuid(),
				metadata = new { created_by = "seed_generator", version = "2.4.1", environment = "staging" },
				telemetry = new { cpu_load = rng.NextDouble() * 100, memory_mb = rng.Next(1024, 8192), active_threads = rng.Next(4, 64) },
				tags = new[] { "prod-ready", "microservice", "high-priority" },
				config = new { retries = 5, circuit_breaker = true, fallback = "cache_v2" }
			}),
			_ => JsonSerializer.Serialize(new
			{
				id = 1000 + index,
				name = $"Entity #{index + 1}",
				email = $"user_{index + 1}@example.com",
				status = (index % 2 == 0 ? "active" : "pending"),
				score = Math.Round(rng.NextDouble() * 100.0, 2),
				updatedAt = DateTime.UtcNow.AddMinutes(-index * 7)
			})
		};

		if (ttl.HasValue && ttl.Value.TotalSeconds > 0)
		{
			await db.StringSetAsync(key, strVal, (Expiration)ttl.Value);
		}
		else
		{
			await db.StringSetAsync(key, strVal);
		}
	}
}

/// <summary>
/// Seeding strategy for Redis Hashes.
/// </summary>
public sealed class HashSeedStrategy : ISeedDataStrategy
{
	public string DataType => "hash";

	public async Task SeedAsync(IDatabase db, string key, string payloadComplexity, int index, TimeSpan? ttl, Random rng)
	{
		HashEntry[] hashEntries = (payloadComplexity ?? "json").ToLowerInvariant() switch
		{
			"simple" => [
				new HashEntry("id", index + 1),
				new HashEntry("status", "ok"),
				new HashEntry("tier", "standard")
			],
			"complex" => [
				new HashEntry("entity_id", Guid.NewGuid().ToString()),
				new HashEntry("organization", "TechCorp Global"),
				new HashEntry("details", JsonSerializer.Serialize(new { quota = 50000, region = "us-east-1", plan = "enterprise" })),
				new HashEntry("security_policy", JsonSerializer.Serialize(new { mfa = true, ip_allowlist = new[] { "192.168.1.1", "10.0.0.1" } })),
				new HashEntry("last_ping_ms", rng.Next(1, 45).ToString())
			],
			_ => [
				new HashEntry("name", $"Service {index + 1}"),
				new HashEntry("role", index % 2 == 0 ? "Administrator" : "Developer"),
				new HashEntry("department", "Engineering"),
				new HashEntry("rating", Math.Round(3.5 + rng.NextDouble() * 1.5, 1).ToString())
			]
		};

		await db.HashSetAsync(key, hashEntries);
		if (ttl.HasValue && ttl.Value.TotalSeconds > 0) await db.KeyExpireAsync(key, ttl.Value);
	}
}

/// <summary>
/// Seeding strategy for Redis Lists.
/// </summary>
public sealed class ListSeedStrategy : ISeedDataStrategy
{
	public string DataType => "list";

	public async Task SeedAsync(IDatabase db, string key, string payloadComplexity, int index, TimeSpan? ttl, Random rng)
	{
		await db.KeyDeleteAsync(key);
		StackExchange.Redis.RedisValue[] listItems = (payloadComplexity ?? "json").ToLowerInvariant() switch
		{
			"simple" => ["item_alpha", "item_beta", "item_gamma"],
			"complex" => [
				JsonSerializer.Serialize(new { event_id = Guid.NewGuid(), action = "cluster_failover", severity = "CRITICAL", ts = DateTime.UtcNow }),
				JsonSerializer.Serialize(new { event_id = Guid.NewGuid(), action = "node_sync_complete", severity = "INFO", ts = DateTime.UtcNow }),
				JsonSerializer.Serialize(new { event_id = Guid.NewGuid(), action = "backup_snapshot", severity = "SUCCESS", ts = DateTime.UtcNow })
			],
			_ => [
				JsonSerializer.Serialize(new { step = 1, desc = "Init queue", done = true }),
				JsonSerializer.Serialize(new { step = 2, desc = "Process batch", done = true }),
				JsonSerializer.Serialize(new { step = 3, desc = "Notify subscribers", done = false })
			]
		};

		await db.ListRightPushAsync(key, listItems);
		if (ttl.HasValue && ttl.Value.TotalSeconds > 0) await db.KeyExpireAsync(key, ttl.Value);
	}
}

/// <summary>
/// Seeding strategy for Redis Sets.
/// </summary>
public sealed class SetSeedStrategy : ISeedDataStrategy
{
	public string DataType => "set";

	public async Task SeedAsync(IDatabase db, string key, string payloadComplexity, int index, TimeSpan? ttl, Random rng)
	{
		await db.KeyDeleteAsync(key);
		StackExchange.Redis.RedisValue[] setMembers = (payloadComplexity ?? "json").ToLowerInvariant() switch
		{
			"simple" => ["tag:alpha", "tag:beta", "tag:gamma", "tag:delta"],
			"complex" => [$"perm:read:{index}", $"perm:write:{index}", $"perm:admin:{index}", $"role:secops:{index}"],
			_ => ["feature:dark_mode", "feature:biometrics", "feature:cloud_sync", "feature:fast_checkout"]
		};

		await db.SetAddAsync(key, setMembers);
		if (ttl.HasValue && ttl.Value.TotalSeconds > 0) await db.KeyExpireAsync(key, ttl.Value);
	}
}

/// <summary>
/// Seeding strategy for Redis Sorted Sets.
/// </summary>
public sealed class SortedSetSeedStrategy : ISeedDataStrategy
{
	public string DataType => "zset";

	public async Task SeedAsync(IDatabase db, string key, string payloadComplexity, int index, TimeSpan? ttl, Random rng)
	{
		await db.KeyDeleteAsync(key);
		SortedSetEntry[] zEntries = [
			new SortedSetEntry($"player_{index}_pro", 8500 + rng.Next(100, 1500)),
			new SortedSetEntry($"player_{index}_elite", 7000 + rng.Next(100, 1000)),
			new SortedSetEntry($"player_{index}_challenger", 5500 + rng.Next(100, 800)),
			new SortedSetEntry($"player_{index}_rookie", 3000 + rng.Next(100, 500))
		];

		await db.SortedSetAddAsync(key, zEntries);
		if (ttl.HasValue && ttl.Value.TotalSeconds > 0) await db.KeyExpireAsync(key, ttl.Value);
	}
}

/// <summary>
/// Seeding strategy for Redis Streams.
/// </summary>
public sealed class StreamSeedStrategy : ISeedDataStrategy
{
	public string DataType => "stream";

	public async Task SeedAsync(IDatabase db, string key, string payloadComplexity, int index, TimeSpan? ttl, Random rng)
	{
		NameValueEntry[] streamFields = [
			new NameValueEntry("event", "telemetry_ping"),
			new NameValueEntry("sensor_id", $"sn_{100 + index}"),
			new NameValueEntry("temperature", Math.Round(20.0 + rng.NextDouble() * 15.0, 1).ToString()),
			new NameValueEntry("pressure_hpa", (1013 + rng.Next(-20, 20)).ToString()),
			new NameValueEntry("status", "nominal")
		];

		await db.StreamAddAsync(key, streamFields);
		if (ttl.HasValue && ttl.Value.TotalSeconds > 0) await db.KeyExpireAsync(key, ttl.Value);
	}
}

/// <summary>
/// Factory for resolving data type seeding strategies.
/// </summary>
public static class SeedDataStrategyFactory
{
	private static readonly Dictionary<string, ISeedDataStrategy> Strategies = new(StringComparer.OrdinalIgnoreCase)
	{
		["string"] = new StringSeedStrategy(),
		["hash"] = new HashSeedStrategy(),
		["list"] = new ListSeedStrategy(),
		["set"] = new SetSeedStrategy(),
		["zset"] = new SortedSetSeedStrategy(),
		["stream"] = new StreamSeedStrategy()
	};

	private static readonly ISeedDataStrategy DefaultStrategy = new StringSeedStrategy();

	/// <summary>
	/// Resolves the seeding strategy for a Redis type name.
	/// </summary>
	public static ISeedDataStrategy GetStrategy(string? type)
	{
		if (!string.IsNullOrWhiteSpace(type) && Strategies.TryGetValue(type, out var strategy))
		{
			return strategy;
		}
		return DefaultStrategy;
	}
}
