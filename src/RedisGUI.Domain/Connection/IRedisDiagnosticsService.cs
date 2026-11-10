using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service responsible for querying Redis server diagnostics, telemetries, slowlog, memory usage, and configuration.
/// </summary>
public interface IRedisDiagnosticsService
{
	/// <summary>
	/// Gets the memory usage of a specific key in bytes.
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="key">The key name.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Key memory usage in bytes.</returns>
	Task<Result<long>> GetKeyMemoryUsageAsync(
		RedisConnection connection,
		string key,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the keyspace statistics (keys count per database).
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Map of database index to number of keys.</returns>
	Task<Result<Dictionary<int, long>>> GetKeyspaceInfoAsync(
		RedisConnection connection,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves slowlog entries from the Redis instance.
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="count">Maximum number of entries to fetch.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of slowlog entries.</returns>
	Task<Result<List<SlowlogEntryInfo>>> GetSlowlogAsync(
		RedisConnection connection,
		int count = 25,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves parsed Redis server statistics and telemetry (INFO command).
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Parsed server information and telemetry.</returns>
	Task<Result<RedisServerInfoDto>> GetServerInfoAsync(
		RedisConnection connection,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves Redis server configuration parameters (CONFIG GET).
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="pattern">Optional configuration parameter pattern.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Server configuration parameters.</returns>
	Task<Result<RedisServerConfigDto>> GetServerConfigAsync(
		RedisConnection connection,
		string? pattern = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates a Redis server configuration parameter (CONFIG SET).
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="parameterName">Configuration parameter name.</param>
	/// <param name="parameterValue">New configuration value.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Result indicating whether configuration update succeeded.</returns>
	Task<Result<RedisSetConfigResponseDto>> SetServerConfigAsync(
		RedisConnection connection,
		string parameterName,
		string parameterValue,
		CancellationToken cancellationToken = default);
}
