using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RedisGUI.Infrastructure.SignalR
{
	/// <summary>
	/// Manages SignalR hub subscriber connections and their associated Redis connection IDs
	/// </summary>
	public class HubSubscribersManager: IHubSubscribersManager
	{
		private readonly ConcurrentDictionary<string, Guid> subscribers = [];

		/// <summary>
		/// Adds a new client connection with its associated Redis connection ID
		/// </summary>
		/// <param name="clientId">The SignalR client connection ID</param>
		/// <param name="redisConnectionId">The associated Redis connection ID</param>
		public void AddConnection(string clientId, Guid redisConnectionId)
		{
			subscribers.TryAdd(clientId, redisConnectionId);
		}

		/// <summary>
		/// Removes a client connection
		/// </summary>
		/// <param name="clientId">The SignalR client connection ID to remove</param>
		public void RemoveConnection(string clientId)
		{
			subscribers.TryRemove(clientId, out var _);
		}

		/// <summary>
		/// Gets a read-only view of all current subscriber connections
		/// </summary>
		/// <returns>A dictionary mapping client IDs to Redis connection IDs</returns>
		public ReadOnlyDictionary<string, Guid> GetSubscribersMap() => this.subscribers.AsReadOnly();
	}
}
