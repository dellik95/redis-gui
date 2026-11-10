using System;

namespace RedisGUI.Application.Redis.Queries.GetRedisValue;

/// <summary>
/// Response containing a single Redis key-value pair with its type, TTL, and structured value.
/// </summary>
/// <param name="Key">The Redis key.</param>
/// <param name="Value">The Redis structured value or string.</param>
/// <param name="Type">The Redis data type.</param>
/// <param name="TimeToLive">Time until the key expires (null if no expiration).</param>
/// <param name="Ttl">TTL in seconds (-1 if no expiration).</param>
public sealed record GetRedisValueResponse(
    string Key,
    object Value,
    string Type,
    TimeSpan? TimeToLive,
    long? Ttl = null);
