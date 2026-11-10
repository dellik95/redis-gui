using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.Parsers;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service responsible for querying Redis server diagnostics, telemetries, slowlog, memory usage, and runtime configuration.
/// </summary>
public class RedisDiagnosticsService(
	IConnectionPool connectionPool,
	ILogger<RedisDiagnosticsService> logger) : IRedisDiagnosticsService
{
	/// <inheritdoc />
	public async Task<Result<long>> GetKeyMemoryUsageAsync(
		RedisConnection connection,
		string key,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<long>(multiplexerResult.Error);
		}

		IDatabase db = multiplexerResult.Value.GetDatabase(connection.DatabaseNumber.Value);
		try
		{
			RedisResult result = await db.ExecuteAsync("MEMORY", "USAGE", key);
			if (result.IsNull)
			{
				return Result.Success(0L);
			}
			return Result.Success((long)result);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Could not get memory usage for key {Key}", key);
			return Result.Success(0L);
		}
	}

	/// <inheritdoc />
	public async Task<Result<Dictionary<int, long>>> GetKeyspaceInfoAsync(
		RedisConnection connection,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<Dictionary<int, long>>(multiplexerResult.Error);
		}

		try
		{
			IServer server = multiplexerResult.Value.GetServers().FirstOrDefault(s => s.IsConnected)
				?? throw new InvalidOperationException("No connected server available.");

			string? info = await server.InfoRawAsync("keyspace");
			return Result.Success(RedisInfoParser.ParseKeyspace(info));
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<List<SlowlogEntryInfo>>(multiplexerResult.Error);

		var server = multiplexerResult.Value.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);
		var entries = await server.SlowlogGetAsync(count);
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<RedisServerInfoDto>(multiplexerResult.Error);

		var server = multiplexerResult.Value.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);
		var infoGroups = await server.InfoAsync();

		return Result.Success(RedisInfoParser.ParseServerInfo(infoGroups));
	}

	/// <inheritdoc />
	public async Task<Result<RedisServerConfigDto>> GetServerConfigAsync(
		RedisConnection connection,
		string? pattern = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<RedisServerConfigDto>(multiplexerResult.Error);

		var server = multiplexerResult.Value.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);
		var configs = await server.ConfigGetAsync(pattern ?? "*");
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<RedisSetConfigResponseDto>(multiplexerResult.Error);

		var server = multiplexerResult.Value.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);
		try
		{
			await server.ConfigSetAsync(parameterName, parameterValue);
			return Result.Success(new RedisSetConfigResponseDto(true, "Configuration parameter updated successfully", parameterName, parameterValue));
		}
		catch (Exception ex)
		{
			return Result.Failure<RedisSetConfigResponseDto>(new Error("Config.UpdateFailed", ex.Message));
		}
	}
}
