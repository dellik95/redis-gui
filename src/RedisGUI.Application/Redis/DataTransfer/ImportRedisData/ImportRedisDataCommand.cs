using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

/// <summary>
/// Command to import Redis data from JSON or CSV format.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Data">The data to import.</param>
/// <param name="Format">Format of the data (JSON or CSV).</param>
/// <param name="Overwrite">Whether to overwrite existing keys.</param>
/// <param name="Database">Optional target Redis database.</param>
/// <param name="OnProgress">Optional callback for completed entries.</param>
/// <param name="OperationCancellation">Optional cooperative cancellation for background execution.</param>
[Auditable(AuditAction.DataImported, "Redis")]
public sealed record ImportRedisDataCommand(
	Guid ConnectionId,
	string Data,
	ImportFormat Format,
	bool Overwrite = false,
	int? Database = null,
	Func<ImportRedisDataProgress, Task>? OnProgress = null,
	CancellationToken? OperationCancellation = null) : ICommand<ImportRedisDataResponse>;

public sealed record ImportRedisDataProgress(int Processed, int Total, int Imported, int Skipped, int Failed);

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
	public string? KeyBase64 { get; init; }
	public string? DumpBase64 { get; init; }
	public long? TtlMilliseconds { get; init; }
}

/// <summary>
/// Handles the command to import Redis data.
/// </summary>
internal sealed class ImportRedisDataCommandHandler
	: ICommandHandler<ImportRedisDataCommand, ImportRedisDataResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisSnapshotService snapshots;
	private readonly IConnectionAccessService access;

	/// <summary>
	/// Initializes a new instance of the ImportRedisDataCommandHandler.
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections.</param>
	/// <param name="snapshots">Service for restoring Redis snapshots.</param>
	/// <param name="access">Current user's connection access.</param>
	public ImportRedisDataCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisSnapshotService snapshots, IConnectionAccessService access)
	{
		this.connectionRepository = connectionRepository;
		this.snapshots = snapshots;
		this.access = access;
	}

	/// <summary>
	/// Handles the command to import Redis data.
	/// </summary>
	/// <param name="request">The command request.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A Result containing the response or an error.</returns>
	public async Task<Result<ImportRedisDataResponse>> Handle(
		ImportRedisDataCommand request,
		CancellationToken cancellationToken)
	{
		Result<RedisConnection> connectionResult = await connectionRepository.GetConnectionByIdAsync(
			request.ConnectionId,
			cancellationToken);

		if (connectionResult.IsFailure)
		{
			return Result.Failure<ImportRedisDataResponse>(connectionResult.Error);
		}

		if (!await access.CanAccessAsync(connectionResult.Value))
			return Result.Failure<ImportRedisDataResponse>(new Error("Connection.Forbidden", "Access to the connection is denied."));
		if (connectionResult.Value.IsReadOnly)
			return Result.Failure<ImportRedisDataResponse>(DomainErrors.Connection.ReadOnlyMode);
		int database = request.Database ?? connectionResult.Value.DatabaseNumber.Value;

		List<RedisImportItem> items;

		try
		{
			items = request.Format == ImportFormat.Json
				? ParseJson(request.Data)
				: ParseCsv(request.Data);
		}
		catch (Exception)
		{
			return Result.Failure<ImportRedisDataResponse>(
				DomainErrors.Import.ParseError);
		}

		int importedCount = 0;
		int skippedCount = 0;
		List<string> failedKeys = new();
		int processed = 0;
		bool cancelled = false;
		bool unconfirmed = false;
		var operationToken = request.OperationCancellation ?? cancellationToken;

		foreach (RedisImportItem item in items)
		{
			if (operationToken.IsCancellationRequested) { cancelled = true; break; }
			try
			{
				if (item.KeyBase64 is not null || item.DumpBase64 is not null)
				{
					if (item.KeyBase64 is null || item.DumpBase64 is null || item.TtlMilliseconds is <= 0)
						throw new FormatException("Invalid snapshot fields.");
					byte[] rawKey = Convert.FromBase64String(item.KeyBase64);
					byte[] payload = Convert.FromBase64String(item.DumpBase64);
					TimeSpan? snapshotTtl = item.TtlMilliseconds is long ms ? TimeSpan.FromMilliseconds(ms) : null;
					var restored = await snapshots.RestoreAsync(connectionResult.Value, rawKey, payload, snapshotTtl, request.Overwrite, database, operationToken);
					if (restored.IsFailure) failedKeys.Add(item.KeyBase64);
					else if (restored.Value) importedCount++;
					else skippedCount++;
					continue;
				}

				// Legacy exports only round-trip strings. Never silently restore other types as strings.
				if (!string.Equals(item.Type, "String", StringComparison.OrdinalIgnoreCase))
				{
					failedKeys.Add(item.Key);
					continue;
				}
				if (item.TimeToLive is <= 0) throw new FormatException("Invalid legacy TTL.");
				TimeSpan? ttl = item.TimeToLive.HasValue
					? TimeSpan.FromSeconds(item.TimeToLive.Value)
					: null;

				Result<bool> setResult = await snapshots.SetLegacyStringAsync(
					connectionResult.Value,
					item.Key,
					item.Value,
					ttl, request.Overwrite, database, operationToken);

				if (setResult.IsFailure) failedKeys.Add(item.Key);
				else if (setResult.Value) importedCount++;
				else skippedCount++;
			}
			catch (OperationCanceledException) when (operationToken.IsCancellationRequested) { cancelled = true; unconfirmed = true; break; }
			catch
			{
				failedKeys.Add(item.KeyBase64 ?? item.Key);
			}
			finally
			{
				if (!cancelled)
				{
					processed++;
					if (request.OnProgress is not null) await request.OnProgress(new ImportRedisDataProgress(processed, items.Count, importedCount, skippedCount, failedKeys.Count));
				}
			}
		}

		ImportRedisDataResponse response = new(
			importedCount,
			skippedCount,
			failedKeys.Count,
			failedKeys, cancelled, unconfirmed);

		return Result.Success(response);
	}

	private static List<RedisImportItem> ParseJson(string data)
	{
		List<RedisImportItem>? items = JsonSerializer.Deserialize<List<RedisImportItem>>(data);
		return items ?? [];
	}

	private static List<RedisImportItem> ParseCsv(string data)
	{
		List<RedisImportItem> items = new();
		string[] lines = data.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length <= 1)
		{
			return items;
		}

		bool snapshotFormat = lines[0].StartsWith("KeyBase64,DumpBase64,", StringComparison.OrdinalIgnoreCase);
		for (int i = 1; i < lines.Length; i++)
		{
			string line = lines[i];
			string[] fields = ParseCsvLine(line);

			if (fields.Length >= 2)
			{
				RedisImportItem item = snapshotFormat ? new RedisImportItem
				{
					KeyBase64 = fields[0], DumpBase64 = fields[1], Type = fields.Length > 2 ? fields[2] : "String",
					TtlMilliseconds = fields.Length > 3 && fields[3].Length > 0 ? long.Parse(fields[3], CultureInfo.InvariantCulture) : null
				} : new RedisImportItem
				{
					Key = fields[0],
					Value = fields.Length > 1 ? fields[1] : string.Empty,
					Type = fields.Length > 2 ? fields[2] : "String",
					TimeToLive = fields.Length > 3 && double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double ttl) ? ttl : null
				};

				items.Add(item);
			}
		}

		return items;
	}

	private static string[] ParseCsvLine(string line)
	{
		List<string> fields = new();
		var current = new StringBuilder();
		bool inQuotes = false;

		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];

			if (c == '"')
			{
				if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
				{
					current.Append('"');
					i++;
				}
				else
				{
					inQuotes = !inQuotes;
				}
			}
			else if (c == ',' && !inQuotes)
			{
				fields.Add(current.ToString());
				current.Clear();
			}
			else
			{
				current.Append(c);
			}
		}

		fields.Add(current.ToString());
		return fields.ToArray();
	}
}
