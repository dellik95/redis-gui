using System;
using System.Collections.Generic;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

/// <summary>
/// Command to import Redis data from JSON or CSV format.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Data">The data to import.</param>
/// <param name="Format">Format of the data (JSON or CSV).</param>
/// <param name="Overwrite">Whether to overwrite existing keys.</param>
public sealed record ImportRedisDataCommand(
    Guid ConnectionId,
    string Data,
    ImportFormat Format,
    bool Overwrite = false) : ICommand<ImportRedisDataResponse>;

/// <summary>
/// Import format enumeration.
/// </summary>
public enum ImportFormat
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

/// <summary>
/// Represents a Redis key-value item for import.
/// </summary>
public sealed record RedisImportItem
{
    /// <summary>
    /// Redis key
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Redis value
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Redis type
    /// </summary>
    public string Type { get; init; } = "String";

    /// <summary>
    /// Time to live in seconds
    /// </summary>
    public double? TimeToLive { get; init; }
}
