using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Queries.GetAllRedisValues;

/// <summary>
/// Query to retrieve Redis values with optional pattern matching and pagination.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Pattern">Optional pattern to filter keys (defaults to "*").</param>
/// <param name="PageSize">Optional number of items per page.</param>
/// <param name="PageNumber">Optional page number.</param>
/// <param name="Database">Optional database index (DB 0-15).</param>
public sealed record GetAllRedisKeysQuery(
    Guid ConnectionId, 
    string Pattern = "*", 
    int? PageSize = null, 
    int? PageNumber = null,
    int? Database = null) : IQuery<GetAllRedisValuesResponse>; 
