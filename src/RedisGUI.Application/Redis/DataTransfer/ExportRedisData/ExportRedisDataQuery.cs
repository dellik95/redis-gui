using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

/// <summary>Exports matching or explicitly selected keys from a Redis database.</summary>
public sealed record ExportRedisDataQuery(
	Guid ConnectionId,
	string Pattern = "*",
	ExportFormat Format = ExportFormat.Json,
	int? Database = null,
	Func<int, Task>? OnProgress = null,
	string[]? Keys = null,
	string?[]? KeysBase64 = null) : IQuery<ExportRedisDataResponse>;

internal sealed class ExportRedisDataQueryHandler(IRedisConnectionRepository repository, IConnectionAccessService access,
	IRedisKeyService keys, IRedisSnapshotService snapshots, IExportSessionFactory sessions)
	: IQueryHandler<ExportRedisDataQuery, ExportRedisDataResponse>
{
	public async Task<Result<ExportRedisDataResponse>> Handle(ExportRedisDataQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await repository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure) return Result.Failure<ExportRedisDataResponse>(connectionResult.Error);
		var connection = connectionResult.Value;
		if (!await access.CanAccessAsync(connection))
			return Result.Failure<ExportRedisDataResponse>(new Error("Connection.Forbidden", "Access to the connection is denied."));

		int database = request.Database ?? connection.DatabaseNumber.Value;
		await using var session = await sessions.CreateAsync(cancellationToken);
		await using var writer = new StreamWriter(session.Output, new UTF8Encoding(false), 65536, leaveOpen: true);
		writer.NewLine = "\r\n";
		bool json = request.Format == ExportFormat.Json;
		await writer.WriteAsync(json ? "[\n" : "KeyBase64,DumpBase64,Type,TtlMilliseconds\r\n");
		bool first = true;
		int exported = 0;

		async Task<Error?> WriteKeyAsync(byte[] key)
		{
			if (!await session.AddKeyAsync(key, cancellationToken)) return null;
			var value = await snapshots.CaptureAsync(connection, key, database, cancellationToken);
			if (value.IsFailure)
				return new Error("Export.KeyReadFailed", $"Export stopped because a key could not be read: {value.Error.Message}");
			string keyBase64 = Convert.ToBase64String(key);
			string dumpBase64 = Convert.ToBase64String(value.Value.Payload);
			if (json)
			{
				if (!first) await writer.WriteAsync(",\n");
				var serialized = JsonSerializer.Serialize(new ExportItem(keyBase64, dumpBase64, value.Value.Type, value.Value.TtlMilliseconds));
				await writer.WriteAsync(serialized.AsMemory(), cancellationToken);
			}
			else
			{
				var ttl = value.Value.TtlMilliseconds?.ToString(CultureInfo.InvariantCulture) ?? "";
				await writer.WriteLineAsync($"{Csv(keyBase64)},{Csv(dumpBase64)},{Csv(value.Value.Type)},{Csv(ttl)}".AsMemory(), cancellationToken);
			}
			first = false;
			exported++;
			if (request.OnProgress is not null) await request.OnProgress(exported);
			return null;
		}

		if (request.Keys is { Length: > 0 })
		{
			for (int i = 0; i < request.Keys.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				byte[] rawKey;
				if (request.KeysBase64 is not null && i < request.KeysBase64.Length && !string.IsNullOrWhiteSpace(request.KeysBase64[i]))
				{
					try
					{
						rawKey = Convert.FromBase64String(request.KeysBase64[i]!);
					}
					catch (FormatException)
					{
						return Result.Failure<ExportRedisDataResponse>(new Error("Export.InvalidKeyBase64", "Invalid Base64 key supplied for export."));
					}
				}
				else
				{
					rawKey = Encoding.UTF8.GetBytes(request.Keys[i]);
				}

				var err = await WriteKeyAsync(rawKey);
				if (err is not null) return Result.Failure<ExportRedisDataResponse>(err);
			}
		}
		else
		{
			string? cursor = null;
			do
			{
				cancellationToken.ThrowIfCancellationRequested();
				var page = await keys.ScanKeysAsync(connection, request.Pattern, database, cursor, 100, cancellationToken);
				if (page.IsFailure) return Result.Failure<ExportRedisDataResponse>(page.Error);
				if (page.Value.MissingCount > 0)
					return Result.Failure<ExportRedisDataResponse>(new Error("Export.KeyReadFailed", "Export stopped because a scanned key disappeared. Retry the export."));
				foreach (var item in page.Value.Keys)
				{
					cancellationToken.ThrowIfCancellationRequested();
					byte[] key = item.RawKey ?? Encoding.UTF8.GetBytes(item.Key);
					var err = await WriteKeyAsync(key);
					if (err is not null) return Result.Failure<ExportRedisDataResponse>(err);
				}
				cursor = page.Value.NextCursor;
			} while (cursor is not null);
		}

		if (json) await writer.WriteAsync("\n]");
		await writer.FlushAsync(cancellationToken);
		session.Output.Position = 0;
		string extension = json ? "json" : "csv";
		return Result.Success(new ExportRedisDataResponse(session.DetachOutput(), $"redis_export_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}",
			json ? "application/json" : "text/csv"));
	}

	private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
	private sealed record ExportItem(string KeyBase64, string DumpBase64, string Type, long? TtlMilliseconds);
}
