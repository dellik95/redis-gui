using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RedisGUI.Infrastructure.SignalR
{
	public class RedisMetricsHubConnectionManager : IRedisMetricsHubConnectionManager
	{
		private readonly ConcurrentDictionary<string, HashSet<Guid>> connectionsMap = new();

		public void AddConnection(string clientId, Guid redisConnectionId)
		{
			connectionsMap.AddOrUpdate(clientId, key => [redisConnectionId],
				(s, set) =>
				{
					set.Add(redisConnectionId);
					return set;
				});
		}

		public void RemoveConnection(string clientId)
		{
			this.connectionsMap.TryRemove(clientId, out var _);
		}

		public IEnumerable<(string clientId, Guid redisConnectionId)> GetAllConnections()
		{
			return this.connectionsMap.SelectMany(item => item.Value.Select(x => (item.Key, x)));
		}
	}
}
