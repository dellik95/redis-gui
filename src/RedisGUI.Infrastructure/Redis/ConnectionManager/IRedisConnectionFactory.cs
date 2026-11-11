using System.IO;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.ConnectionManager;

/// <summary>
/// Abstraction for creating <see cref="ConnectionMultiplexer"/> instances.
/// Allows unit and integration tests to control connection lifecycle and synchronization.
/// </summary>
public interface IRedisConnectionFactory
{
	/// <summary>
	/// Establishes a connection asynchronously to the specified Redis configuration options.
	/// </summary>
	Task<ConnectionMultiplexer> ConnectAsync(ConfigurationOptions options, TextWriter? log = null);
}
