using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Queries.GetKeyMemoryUsage;

/// <summary>
/// Query to retrieve memory usage in bytes for a specific Redis key
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier</param>
/// <param name="Key">The Redis key name</param>
public sealed record GetKeyMemoryUsageQuery(
    Guid ConnectionId,
    string Key) : IQuery<GetKeyMemoryUsageResponse>;
