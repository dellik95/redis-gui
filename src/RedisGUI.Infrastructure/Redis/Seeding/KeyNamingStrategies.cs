namespace RedisGUI.Infrastructure.Redis.Seeding;

/// <summary>
/// Generates simple flat keys (e.g. "demo:string:1").
/// </summary>
public sealed class SimpleKeyNamingStrategy : IKeyNamingStrategy
{
	public string StrategyName => "simple";

	public string GenerateKeyName(string prefix, string type, int index, Random rng) =>
		$"{prefix}:{type.ToLowerInvariant()}:{index + 1}";
}

/// <summary>
/// Generates UUID-based keys (e.g. "demo:string:b08d2987-...").
/// </summary>
public sealed class UuidKeyNamingStrategy : IKeyNamingStrategy
{
	public string StrategyName => "uuid";

	public string GenerateKeyName(string prefix, string type, int index, Random rng) =>
		$"{prefix}:{type.ToLowerInvariant()}:{Guid.NewGuid():D}";
}

/// <summary>
/// Generates Redis cluster hash-tag keys (e.g. "demo:{tenant_1}:string:1").
/// </summary>
public sealed class HashtagKeyNamingStrategy : IKeyNamingStrategy
{
	public string StrategyName => "hashtag";

	public string GenerateKeyName(string prefix, string type, int index, Random rng) =>
		$"{prefix}:{{tenant_{index % 5 + 1}}}:{type.ToLowerInvariant()}:{index + 1}";
}

/// <summary>
/// Generates realistic multi-level enterprise hierarchical keys.
/// </summary>
public sealed class HierarchicalKeyNamingStrategy : IKeyNamingStrategy
{
	public string StrategyName => "hierarchical";

	public string GenerateKeyName(string prefix, string type, int index, Random rng) =>
		type.ToLowerInvariant() switch
		{
			"string" => $"{prefix}:corp:finance:accounts:acc_{1000 + index}:balance",
			"hash" => $"{prefix}:ecommerce:customers:cust_{2000 + index}:profile",
			"list" => $"{prefix}:infra:cluster:node_{index % 8 + 1}:activity_log",
			"set" => $"{prefix}:security:roles:role_{index % 10 + 1}:permissions",
			"zset" => $"{prefix}:gaming:leaderboard:season_{index % 4 + 1}:rankings",
			"stream" => $"{prefix}:telemetry:sensors:probe_{index % 6 + 1}:events",
			_ => $"{prefix}:app:service:module_{index % 5 + 1}:{type.ToLowerInvariant()}:{index + 1}"
		};
}

/// <summary>
/// Factory for resolving key naming strategies.
/// </summary>
public static class KeyNamingStrategyFactory
{
	private static readonly Dictionary<string, IKeyNamingStrategy> Strategies = new(StringComparer.OrdinalIgnoreCase)
	{
		["simple"] = new SimpleKeyNamingStrategy(),
		["uuid"] = new UuidKeyNamingStrategy(),
		["hashtag"] = new HashtagKeyNamingStrategy(),
		["hierarchical"] = new HierarchicalKeyNamingStrategy()
	};

	private static readonly IKeyNamingStrategy DefaultStrategy = new HierarchicalKeyNamingStrategy();

	/// <summary>
	/// Resolves the naming strategy by name, defaulting to hierarchical if unknown.
	/// </summary>
	public static IKeyNamingStrategy GetStrategy(string? complexity)
	{
		if (!string.IsNullOrWhiteSpace(complexity) && Strategies.TryGetValue(complexity, out var strategy))
		{
			return strategy;
		}
		return DefaultStrategy;
	}
}
