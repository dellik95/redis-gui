using System;
using RedisGUI.Domain.Primitives;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Defines a connection pool for managing Redis connections
/// </summary>
public interface IConnectionPool : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Retrieves a connection from the pool or creates a new one if necessary
    /// </summary>
    /// <param name="configuration">Redis configuration options</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A Result containing either a successful ConnectionMultiplexer or an error</returns>
    Task<Result<ConnectionMultiplexer>> GetConnection(
        ConfigurationOptions configuration,
        CancellationToken cancellationToken = default);
}
