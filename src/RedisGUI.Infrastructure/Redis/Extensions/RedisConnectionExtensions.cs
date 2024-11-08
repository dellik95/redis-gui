using RedisGUI.Domain.Connection;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Extensions;

/// <summary>
/// Extension methods for Redis connection configuration
/// </summary>
public static class RedisConnectionExtensions
{
    /// <summary>
    /// Converts a RedisConnection object to StackExchange.Redis ConfigurationOptions
    /// </summary>
    /// <param name="connection">The Redis connection to convert</param>
    /// <returns>A ConfigurationOptions object configured with the connection settings</returns>
    public static ConfigurationOptions ToConfigurationOptions(this RedisConnection connection)
    {
        //TODO: Add Login And Password
        return new ConfigurationOptions
        {
            EndPoints = new EndPointCollection
            {
                connection.BuildConnectionString()
            },
            AbortOnConnectFail = true,
            AllowAdmin = true,
            ConnectTimeout = 10
        };
    }
} 