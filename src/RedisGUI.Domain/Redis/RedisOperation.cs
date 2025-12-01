namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents a single Redis operation for transactions
/// </summary>
public sealed record RedisOperation(
	string Command,
	string Key,
	string? Value = null,
	string[]? Arguments = null);
