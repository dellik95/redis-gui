using System;
using RedisGUI.Shared.Mediator;

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

/// <summary>
/// Export format enumeration.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON format
    /// </summary>
    Json = 0,

    /// <summary>
    /// CSV format
    /// </summary>
    Csv = 1
}
