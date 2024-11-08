using System;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Configuration options for the Redis connection pool
/// </summary>
public class ConnectionPoolOptions
{
    /// <summary>
    /// Configuration key for the Redis connection pool settings
    /// </summary>
    public const string Key = "Redis:ConnectionPool";

    /// <summary>
    /// Maximum number of concurrent connections allowed in the pool
    /// </summary>
    public int MaxConnections { get; set; } = 50;

    /// <summary>
    /// Timeout duration for establishing new connections
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Duration after which idle connections are removed from the pool
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
} 
