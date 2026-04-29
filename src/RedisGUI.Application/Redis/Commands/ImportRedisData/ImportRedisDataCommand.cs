using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System;

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
