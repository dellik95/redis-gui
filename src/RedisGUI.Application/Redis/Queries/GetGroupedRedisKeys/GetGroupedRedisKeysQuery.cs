using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Queries.GetGroupedRedisKeys;

/// <summary>
/// Query to retrieve Redis keys grouped by delimiter in flat or tree structure.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Pattern">Optional pattern to filter keys (defaults to "*").</param>
/// <param name="Delimiter">Delimiter for grouping keys (e.g., ":", "/", "-").</param>
/// <param name="ViewMode">View mode: "flat" or "tree".</param>
public sealed record GetGroupedRedisKeysQuery(
    Guid ConnectionId,
    string Pattern = "*",
    string Delimiter = ":",
    string ViewMode = "tree") : IQuery<GetGroupedRedisKeysResponse>;
