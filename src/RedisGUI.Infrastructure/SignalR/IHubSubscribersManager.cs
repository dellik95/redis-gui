using System.Collections.ObjectModel;
using System;

namespace RedisGUI.Infrastructure.SignalR
{
	/// <summary>
	/// Defines operations for managing SignalR hub subscriber connections
	/// </summary>
	public interface IHubSubscribersManager
	{
		/// <summary>
		/// Adds a new client connection with its associated Redis connection ID
		/// </summary>
		/// <param name="clientId">The SignalR client connection ID</param>
		/// <param name="redisConnectionId">The associated Redis connection ID</param>
		public void AddConnection(string clientId, Guid redisConnectionId);

		/// <summary>
		/// Removes a client connection
		/// </summary>
		/// <param name="clientId">The SignalR client connection ID to remove</param>
		public void RemoveConnection(string clientId);

		/// <summary>
		/// Gets a read-only view of all current subscriber connections
		/// </summary>
		/// <returns>A dictionary mapping client IDs to Redis connection IDs</returns>
		public ReadOnlyDictionary<string, Guid> GetSubscribersMap();
	}
}
