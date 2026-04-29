using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

/// <summary>
/// Query to export Redis data in a specified format.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Pattern">Pattern to filter keys (defaults to "*").</param>
/// <param name="Format">Export format (JSON or CSV).</param>
public sealed record ExportRedisDataQuery(
	Guid ConnectionId,
	string Pattern = "*",
	ExportFormat Format = ExportFormat.Json) : IQuery<ExportRedisDataResponse>;
