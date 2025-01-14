using System;
using System.Collections.Generic;

namespace RedisGUI.Infrastructure.SignalR;

public interface IRedisMetricsHubConnectionManager
{
	void AddConnection(string clientId, Guid redisConnectionId);

	void RemoveConnection(string clientId);

	IEnumerable<(string clientId, Guid redisConnectionId)> GetAllConnections();
}
