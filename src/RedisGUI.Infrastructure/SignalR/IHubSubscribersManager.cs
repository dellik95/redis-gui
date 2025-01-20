using System.Collections.ObjectModel;
using System;

namespace RedisGUI.Infrastructure.SignalR
{
	public interface IHubSubscribersManager
	{
		public void AddConnection(string clientId, Guid redisConnectionId);

		public void RemoveConnection(string clientId);

		public ReadOnlyDictionary<string, Guid> GetSubscribersMap();
	}
}
