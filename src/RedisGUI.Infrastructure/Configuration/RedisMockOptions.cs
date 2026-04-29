namespace RedisGUI.Infrastructure.Configuration;

/// <summary>
/// Configuration options for enabling backend Redis mock responses.
/// </summary>
public sealed class RedisMockOptions
{
	public const string Key = "Redis:Mock";

	/// <summary>
	/// When true, IConnectionService will use an in-memory mock instead of real Redis.
	/// </summary>
	public bool Enabled { get; set; } = false;

	/// <summary>
	/// When true, seeds the mock store with dummy data on initialization.
	/// </summary>
	public bool SeedDummyData { get; set; } = true;

	/// <summary>
	/// Number of dummy keys to seed when SeedDummyData is enabled.
	/// </summary>
	public int DummyDataKeyCount { get; set; } = 100;

	/// <summary>
	/// When true, enables TTL expiration handling with background cleanup.
	/// </summary>
	public bool EnableTtlExpiration { get; set; } = true;
}

