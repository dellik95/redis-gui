using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System;
using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Queries.GetSlowlog;

/// <summary>
/// Query to retrieve Redis slowlog entries
/// </summary>
public sealed record GetSlowlogQuery(Guid ConnectionId, int Count = 25) : IQuery<List<SlowlogEntryInfo>>;
