using System;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents a Redis key-value pair with metadata
/// </summary>
/// <param name="Key">The Redis key</param>
/// <param name="Value">The Redis value</param>
/// <param name="Type">The Redis data type</param>
/// <param name="TimeToLive">The time-to-live for the key-value pair</param>
public record RedisValue(
	string Key,
	string Value,
	string Type,
	TimeSpan? TimeToLive);
