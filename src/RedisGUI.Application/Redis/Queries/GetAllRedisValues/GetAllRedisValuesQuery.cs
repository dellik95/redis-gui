using RedisGUI.Application.Abstraction.Messaging;
using System;

namespace RedisGUI.Application.Redis.Queries.GetAllRedisValues;

/// <summary>
/// Query to retrieve Redis values with optional pattern matching and pagination.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Pattern">Optional pattern to filter keys (defaults to "*").</param>
/// <param name="PageSize">Optional number of items per page.</param>
/// <param name="PageNumber">Optional page number.</param>
public sealed record GetAllRedisValuesQuery(
    Guid ConnectionId, 
    string Pattern = "*", 
    int? PageSize = null, 
    int? PageNumber = null) : IQuery<GetAllRedisValuesResponse>; 
