using RedisGUI.Application.Abstraction.Messaging;
using System;

namespace RedisGUI.Application.Redis.Commands.SetRedisValue;

/// <summary>
/// Command to set a Redis key-value pair with optional TTL.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Key">The key to set.</param>
/// <param name="Value">The value to set.</param>
/// <param name="Ttl">Optional time-to-live for the key.</param>
public sealed record AddRedisValueCommand(
    Guid ConnectionId,
    string Key,
    string Value,
    TimeSpan? Ttl = null) : ICommand; 
