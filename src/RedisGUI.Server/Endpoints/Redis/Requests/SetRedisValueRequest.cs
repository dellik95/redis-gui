using System;

namespace RedisGUI.Server.Endpoints.Redis.Requests;

/// <summary>
/// Request model for setting Redis value
/// </summary>
public sealed class SetRedisValueRequest
{
    /// <summary>
    /// Redis key
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Redis value
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Time to live in seconds (optional)
    /// </summary>
    public TimeSpan? Ttl { get; init; }
} 
