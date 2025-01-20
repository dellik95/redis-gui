using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RedisGUI.Infrastructure.SignalR
{
	public class HubSubscribersManager: IHubSubscribersManager
	{
		private readonly ConcurrentDictionary<string, Guid> subscribers = [];

		public void AddConnection(string clientId, Guid redisConnectionId)
		{
			subscribers.TryAdd(clientId, redisConnectionId);
		}

		public void RemoveConnection(string clientId)
		{
			subscribers.TryRemove(clientId, out var _);
		}

		public ReadOnlyDictionary<string, Guid> GetSubscribersMap() => this.subscribers.AsReadOnly();
	}
}
