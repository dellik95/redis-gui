using System.Linq;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>Resolves a server from the active multiplexer, including fallback connections.</summary>
internal static class RedisServerResolver
{
	internal static IServer GetConnectedServer(this ConnectionMultiplexer multiplexer, RedisConnection connection)
	{
		try
		{
			return multiplexer.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);
		}
		catch (ArgumentException)
		{
			var servers = multiplexer.GetServers().Where(server => server.IsConnected).ToArray();
			return servers.FirstOrDefault(server => !server.IsReplica)
				?? servers.FirstOrDefault()
				?? throw new InvalidOperationException("No connected Redis server is available.");
		}
	}
}
