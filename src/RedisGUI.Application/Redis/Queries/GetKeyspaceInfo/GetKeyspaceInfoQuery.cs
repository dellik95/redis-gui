using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Queries.GetKeyspaceInfo;

/// <summary>
/// Query to retrieve Redis keyspace information (key counts across databases DB 0-15)
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier</param>
public sealed record GetKeyspaceInfoQuery(Guid ConnectionId) : IQuery<GetKeyspaceInfoResponse>;
