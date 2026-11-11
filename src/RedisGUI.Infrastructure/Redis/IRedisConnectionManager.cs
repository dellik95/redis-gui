using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Manages cached, shared <see cref="ConnectionMultiplexer"/> instances across multiple Redis servers.
/// Provides thread-safe single-flight initialization, graceful eviction, and observable runtime state.
/// </summary>
public interface IRedisConnectionManager : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Retrieves an existing shared ConnectionMultiplexer for the specified configuration or creates a new one.
	/// </summary>
	Task<Result<ConnectionMultiplexer>> GetConnection(
		string configuration,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves an existing shared ConnectionMultiplexer for the specified key and configuration or creates a new one.
	/// </summary>
	Task<Result<ConnectionMultiplexer>> GetConnection(
		string key,
		string configuration,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Explicitly invalidates and safely retires the connection associated with the specified server key.
	/// </summary>
	Task InvalidateConnectionAsync(
		string key,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets aggregate telemetry and metrics for the connection manager.
	/// </summary>
	RedisConnectionManagerStatistics GetStatistics();

	/// <summary>
	/// Gets snapshots of runtime connection states for all registered servers.
	/// </summary>
	IReadOnlyDictionary<string, RedisConnectionInfo> GetConnectionStates();
}
