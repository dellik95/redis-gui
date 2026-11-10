namespace RedisGUI.Infrastructure.Redis.Seeding;

/// <summary>
/// Strategy interface for generating key names during demo data seeding.
/// </summary>
public interface IKeyNamingStrategy
{
	/// <summary>
	/// Gets the strategy identifier (e.g. "simple", "uuid", "hashtag", "hierarchical").
	/// </summary>
	string StrategyName { get; }

	/// <summary>
	/// Generates a key name according to the naming pattern.
	/// </summary>
	string GenerateKeyName(string prefix, string type, int index, Random rng);
}
