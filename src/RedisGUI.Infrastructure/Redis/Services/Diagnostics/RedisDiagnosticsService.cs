using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.Parsers;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service responsible for querying Redis server diagnostics, telemetries, slowlog, memory usage, and runtime configuration
/// across both standalone and Redis Cluster deployments.
/// </summary>
public class RedisDiagnosticsService(
	IRedisConnectionManager connectionManager,
	ILogger<RedisDiagnosticsService> logger,
	IRedisTopologyRegistry? topology = null) : IRedisDiagnosticsService
{
	private Task<Result<ConnectionMultiplexer>> GetConnectionAsync(RedisConnection connection, CancellationToken cancellationToken) =>
		connectionManager.GetConnectionAsync(connection, topology, cancellationToken);

	/// <inheritdoc />
	public async Task<Result<long>> GetKeyMemoryUsageAsync(
		RedisConnection connection,
		string key,
		CancellationToken cancellationToken = default,
		int? database = null,
		string? keyBase64 = null)
	{
		var resolvedKey = RedisServerResolver.ResolveKey(key, keyBase64);
		if (resolvedKey.IsFailure) return Result.Failure<long>(resolvedKey.Error);

		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<long>(multiplexerResult.Error);
		}

		int targetDb = multiplexerResult.Value.IsCluster() ? 0 : (database ?? connection.DatabaseNumber.Value);
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		try
		{
			RedisResult result = await db.ExecuteAsync("MEMORY", "USAGE", resolvedKey.Value).WaitAsync(cancellationToken);
			if (result.IsNull)
			{
				return Result.Success(0L);
			}
			return Result.Success((long)result);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Could not get memory usage for key {Key} in DB {Database}", key, targetDb);
			return Result.Success(0L);
		}
	}

	/// <inheritdoc />
	public async Task<Result<Dictionary<int, long>>> GetKeyspaceInfoAsync(
		RedisConnection connection,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<Dictionary<int, long>>(multiplexerResult.Error);
		}

		try
		{
			var multiplexer = multiplexerResult.Value;
			if (multiplexer.IsCluster())
			{
				var primaries = multiplexer.GetConnectedPrimaryServers();
				if (primaries.Length == 0)
				{
					primaries = multiplexer.GetServers().Where(s => s.IsConnected).ToArray();
				}

				var merged = RedisInfoParser.ParseKeyspace(null);
				var infos = await Task.WhenAll(primaries.Select(s => s.InfoRawAsync("keyspace").WaitAsync(cancellationToken)));
				foreach (var info in infos)
				{
					var parsed = RedisInfoParser.ParseKeyspace(info);
					foreach (var kvp in parsed)
					{
						merged[kvp.Key] = merged.GetValueOrDefault(kvp.Key) + kvp.Value;
					}
				}
				return Result.Success(merged);
			}

			IServer server = multiplexer.GetConnectedServer(connection);
			string? raw = await server.InfoRawAsync("keyspace").WaitAsync(cancellationToken);
			return Result.Success(RedisInfoParser.ParseKeyspace(raw));
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to get keyspace info");
			return Result.Success(RedisInfoParser.ParseKeyspace(null));
		}
	}

	/// <inheritdoc />
	public async Task<Result<List<SlowlogEntryInfo>>> GetSlowlogAsync(
		RedisConnection connection,
		int count = 25,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<List<SlowlogEntryInfo>>(multiplexerResult.Error);

		var multiplexer = multiplexerResult.Value;
		if (multiplexer.IsCluster())
		{
			var nodes = multiplexer.GetConnectedPrimaryServers();
			if (nodes.Length == 0) nodes = multiplexer.GetServers().Where(s => s.IsConnected).ToArray();

			var allEntries = new List<SlowlogEntryInfo>();
			foreach (var node in nodes)
			{
				try
				{
					var nodeEntries = await node.SlowlogGetAsync(count).WaitAsync(cancellationToken);
					string endpoint = node.EndPoint.ToString() ?? string.Empty;
					allEntries.AddRange(nodeEntries.Select(e => new SlowlogEntryInfo(
						e.UniqueId,
						e.Time,
						e.Duration,
						e.Arguments.Select(a => a.ToString()).ToArray(),
						endpoint,
						string.Empty)));
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Could not retrieve slowlog from cluster node {Endpoint}", node.EndPoint);
				}
			}

			return Result.Success(allEntries
				.OrderByDescending(e => e.Timestamp)
				.Take(count)
				.ToList());
		}

		var server = multiplexer.GetConnectedServer(connection);
		var entries = await server.SlowlogGetAsync(count).WaitAsync(cancellationToken);
		var result = entries.Select(e => new SlowlogEntryInfo(
			e.UniqueId,
			e.Time,
			e.Duration,
			e.Arguments.Select(a => a.ToString()).ToArray(),
			string.Empty,
			string.Empty
		)).ToList();

		return Result.Success(result);
	}

	/// <inheritdoc />
	public async Task<Result<RedisServerInfoDto>> GetServerInfoAsync(
		RedisConnection connection,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<RedisServerInfoDto>(multiplexerResult.Error);

		var multiplexer = multiplexerResult.Value;
		var server = multiplexer.GetConnectedServer(connection);
		var infoGroups = await server.InfoAsync().WaitAsync(cancellationToken);
		var dto = RedisInfoParser.ParseServerInfo(infoGroups);

		if (multiplexer.IsCluster())
		{
			var clusterNodes = multiplexer.GetServers()
				.Where(s => s.ServerType == ServerType.Cluster)
				.OrderBy(s => s.EndPoint.ToString(), StringComparer.Ordinal)
				.ToDictionary(
					s => s.EndPoint.ToString() ?? "unknown",
					s => $"{(s.IsReplica ? "replica" : "primary")} ({(s.IsConnected ? "connected" : "disconnected")})");

			var sections = dto.AllSections.ToList();
			if (clusterNodes.Count > 0)
			{
				sections.Add(new RedisServerInfoSection("ClusterNodes", clusterNodes));
			}

			dto = dto with { RedisMode = "cluster", AllSections = sections };
		}

		return Result.Success(dto);
	}

	/// <inheritdoc />
	public async Task<Result<RedisServerConfigDto>> GetServerConfigAsync(
		RedisConnection connection,
		string? pattern = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<RedisServerConfigDto>(multiplexerResult.Error);

		var server = multiplexerResult.Value.GetConnectedServer(connection);
		var configs = await server.ConfigGetAsync(pattern ?? "*").WaitAsync(cancellationToken);
		var paramList = configs
			.Select(c => new RedisConfigParameterDto(c.Key, c.Value, true))
			.OrderBy(c => c.Name)
			.ToList();

		return Result.Success(new RedisServerConfigDto(paramList, paramList.Count));
	}

	/// <inheritdoc />
	public async Task<Result<RedisSetConfigResponseDto>> SetServerConfigAsync(
		RedisConnection connection,
		string parameterName,
		string parameterValue,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<RedisSetConfigResponseDto>(multiplexerResult.Error);

		try
		{
			var server = multiplexerResult.Value.GetConnectedServer(connection);
			await server.ConfigSetAsync(parameterName, parameterValue).WaitAsync(cancellationToken);
			return Result.Success(new RedisSetConfigResponseDto(true, "Configuration parameter updated successfully", parameterName, parameterValue));
		}
		catch (Exception ex)
		{
			return Result.Failure<RedisSetConfigResponseDto>(new Error("Config.UpdateFailed", ex.Message));
		}
	}
}
