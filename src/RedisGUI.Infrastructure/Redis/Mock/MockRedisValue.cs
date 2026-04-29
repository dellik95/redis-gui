namespace RedisGUI.Infrastructure.Redis.Mock;

/// <summary>
/// Represents a mock Redis value with support for all Redis data types
/// </summary>
public sealed class MockRedisValue
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
/// Represents a stream entry in a mock Redis stream
/// </summary>
public sealed class MockStreamEntry
{
	public string Id { get; set; } = string.Empty;
	public Dictionary<string, string> Fields { get; set; } = [];
}
