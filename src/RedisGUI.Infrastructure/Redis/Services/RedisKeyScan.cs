using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Cursor validation and page retrieval for incremental key discovery.</summary>
internal static class RedisKeyScan
{
	internal sealed record Cursor(Guid ConnectionId, int Database, string Pattern, string[] Nodes, int NodeIndex,
		[property: JsonPropertyName("Cursor")] string Position);
	internal sealed record Context(IServer[] Servers, Cursor State);

	internal static Result ValidateParameters(string pattern, int database, string? cursor, int count) =>
		database is < 0 or > 15 || count is < 1 or > 500 || string.IsNullOrEmpty(pattern) || pattern.Length > 1024 || cursor?.Length > 8192
			? Result.Failure(new Error("Redis.InvalidScan", "Invalid scan parameters."))
			: Result.Success();

	internal static Result<Context> CreateContext(ConnectionMultiplexer multiplexer, Guid connectionId, string pattern, int database, string? cursor)
	{
		IServer[] servers = multiplexer.GetServers()
			.Where(server => !server.IsReplica && server.ServerType != ServerType.Sentinel)
			.OrderBy(server => server.EndPoint.ToString(), StringComparer.Ordinal)
			.ToArray();
		if (servers.Length == 0 || servers.Any(server => !server.IsConnected || !server.Features.Scan))
		{
			return Result.Failure<Context>(new Error("Redis.ScanUnavailable", "A primary Redis server is unavailable or does not support SCAN."));
		}

		if (servers.Length > 1 && servers.Any(server => server.ServerType != ServerType.Cluster))
		{
			return Result.Failure<Context>(new Error("Redis.ScanUnavailable", "Multiple standalone primaries cannot be scanned reliably through one connection."));
		}

		string[] nodes = servers.Select(server => server.EndPoint.ToString()!).ToArray();
		return ParseCursor(cursor, connectionId, database, pattern, nodes)
			.Bind(state => servers[state.NodeIndex].ServerType == ServerType.Cluster && database != 0
				? Result.Failure<Context>(new Error("Redis.InvalidDatabase", "Redis Cluster supports database 0 only."))
				: Result.Success(new Context(servers, state)));
	}

	private static Result<Cursor> ParseCursor(string? encoded, Guid connectionId, int database, string pattern, string[] nodes)
	{
		if (encoded is null)
		{
			return Result.Success(new Cursor(connectionId, database, pattern, nodes, 0, "0"));
		}

		try
		{
			// Keep the serialized property name compatible with existing cursors.
			var stored = JsonSerializer.Deserialize<Cursor>(Convert.FromBase64String(encoded));
			if (stored is null || stored.ConnectionId != connectionId || stored.Database != database || stored.Pattern != pattern ||
				stored.Nodes is null || !stored.Nodes.SequenceEqual(nodes) || stored.NodeIndex < 0 || stored.NodeIndex >= nodes.Length ||
				!ulong.TryParse(stored.Position, out _))
			{
				return Result.Failure<Cursor>(new Error("Redis.InvalidCursor", "Scan cursor is invalid or the Redis topology changed. Refresh to start again."));
			}

			return Result.Success(stored);
		}
		catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
		{
			return Result.Failure<Cursor>(new Error("Redis.InvalidCursor", "Scan cursor is invalid. Refresh to start again."));
		}
	}

	internal static async Task<Result<RedisKeyScanPage>> ReadPageAsync(ConnectionMultiplexer multiplexer, Context context, int count, CancellationToken cancellationToken)
	{
		Cursor state = context.State;
		IServer server = context.Servers[state.NodeIndex];
		IDatabase database = multiplexer.GetDatabase(state.Database);
		object[] args = [state.Position, "MATCH", state.Pattern, "COUNT", count];
		// Cluster SCAN targets a primary; standalone SCAN uses the selected database.
		RedisResult raw = server.ServerType == ServerType.Cluster
			? await server.ExecuteAsync("SCAN", args).WaitAsync(cancellationToken)
			: await database.ExecuteAsync("SCAN", args, CommandFlags.DemandMaster).WaitAsync(cancellationToken);
		RedisResult[] parts = (RedisResult[])raw!;
		string next = (string)parts[0]!;
		RedisResult[] keys = (RedisResult[])parts[1]!;
		RedisKeyScanItem?[] metadata = await Task.WhenAll(keys.Select(key => ReadMetadataAsync(database, (string)key!, cancellationToken)));
		int node = state.NodeIndex + (next == "0" ? 1 : 0);
		string? nextCursor = node >= context.Servers.Length ? null : EncodeCursor(state, node, next);
		return Result.Success(new RedisKeyScanPage(metadata.OfType<RedisKeyScanItem>().ToArray(), nextCursor));
	}

	private static async Task<RedisKeyScanItem?> ReadMetadataAsync(IDatabase database, string key, CancellationToken cancellationToken)
	{
		Task<RedisType> typeTask = database.KeyTypeAsync(key);
		Task<TimeSpan?> ttlTask = database.KeyTimeToLiveAsync(key);
		await Task.WhenAll(typeTask, ttlTask).WaitAsync(cancellationToken);
		return typeTask.Result == RedisType.None ? null : new RedisKeyScanItem(key, typeTask.Result.ToString(), ttlTask.Result?.TotalSeconds);
	}

	private static string EncodeCursor(Cursor state, int node, string next) => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
		state with { NodeIndex = node, Position = next }));
}
