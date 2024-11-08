using System;

namespace RedisGUI.Application.Redis.Queries.GetRedisValue;

/// <summary>
/// Response containing a single Redis key-value pair with its TTL.
/// </summary>
/// <param name="Key">The Redis key.</param>
/// <param name="Value">The Redis value.</param>
/// <param name="TimeToLive">Time until the key expires (null if no expiration).</param>
public sealed record GetRedisValueResponse(
    string Key,
    string Value,
    TimeSpan? TimeToLive); 