using System.IO;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Default implementation that delegates connection creation to StackExchange.Redis <see cref="ConnectionMultiplexer.ConnectAsync(ConfigurationOptions, TextWriter)"/>.
/// </summary>
public sealed class DefaultRedisConnectionFactory : IRedisConnectionFactory
{
	public Task<ConnectionMultiplexer> ConnectAsync(ConfigurationOptions options, TextWriter? log = null)
	{
		return ConnectionMultiplexer.ConnectAsync(options, log);
	}
}
