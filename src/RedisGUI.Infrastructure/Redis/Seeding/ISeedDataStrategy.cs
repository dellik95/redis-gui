using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Seeding;

/// <summary>
/// Strategy interface for seeding a specific Redis data type.
/// </summary>
public interface ISeedDataStrategy
{
	/// <summary>
	/// Target Redis data type name (e.g. "string", "hash", "list", "set", "zset", "stream").
	/// </summary>
	string DataType { get; }

	/// <summary>
	/// Seeds a single key with realistic synthetic payload according to complexity level.
	/// </summary>
	Task SeedAsync(
		IDatabase db,
		string key,
		string payloadComplexity,
		int index,
		TimeSpan? ttl,
		Random rng);
}
