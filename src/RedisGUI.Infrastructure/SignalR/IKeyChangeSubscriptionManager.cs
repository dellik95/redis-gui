namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Manages SignalR client subscriptions to key change notifications
/// </summary>
public interface IKeyChangeSubscriptionManager
{
	/// <summary>
	/// Subscribes a client to key changes for a Redis connection
	/// </summary>
	/// <param name="clientId">The SignalR client connection ID</param>
	/// <param name="redisConnectionId">The Redis connection ID to watch</param>
	void SubscribeToConnection(string clientId, string redisConnectionId);

	/// <summary>
	/// Unsubscribes a client from key changes for a Redis connection
	/// </summary>
	/// <param name="clientId">The SignalR client connection ID</param>
	/// <param name="redisConnectionId">The Redis connection ID to stop watching</param>
	void UnsubscribeFromConnection(string clientId, string redisConnectionId);

	/// <summary>
	/// Unsubscribes a client from all connections
	/// </summary>
	/// <param name="clientId">The SignalR client connection ID</param>
	void UnsubscribeClient(string clientId);

	/// <summary>
	/// Gets all client IDs watching a specific Redis connection
	/// </summary>
	/// <param name="redisConnectionId">The Redis connection ID</param>
	/// <returns>Collection of client IDs</returns>
	IEnumerable<string> GetClientsForConnection(string redisConnectionId);
}

