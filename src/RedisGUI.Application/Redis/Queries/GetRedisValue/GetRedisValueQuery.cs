using RedisGUI.Application.Abstraction.Messaging;
using System;

namespace RedisGUI.Application.Redis.Queries.GetRedisValue;

/// <summary>
/// Query to retrieve a single Redis value by key.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Key">The key to retrieve.</param>
public sealed record GetRedisValueQuery(Guid ConnectionId, string Key) : IQuery<GetRedisValueResponse>;
