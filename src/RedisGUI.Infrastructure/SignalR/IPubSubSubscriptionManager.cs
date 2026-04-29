namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Manages SignalR client subscriptions to Pub/Sub channels
/// </summary>
public interface IPubSubSubscriptionManager
{
	/// <summary>
	/// Adds a client to a subscription group
	/// </summary>
	/// <param name="clientId">The SignalR client connection ID</param>
	/// <param name="subscriptionId">The subscription identifier</param>
	void AddClientToSubscription(string clientId, string subscriptionId);

	/// <summary>
	/// Removes a client from a subscription group
	/// </summary>
	/// <param name="clientId">The SignalR client connection ID</param>
	/// <param name="subscriptionId">The subscription identifier</param>
	void RemoveClientFromSubscription(string clientId, string subscriptionId);

	/// <summary>
	/// Removes all subscriptions for a client
	/// </summary>
	/// <param name="clientId">The SignalR client connection ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task RemoveClientSubscriptions(string clientId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all client IDs subscribed to a specific subscription
	/// </summary>
	/// <param name="subscriptionId">The subscription identifier</param>
	/// <returns>Collection of client IDs</returns>
	IEnumerable<string> GetClientsForSubscription(string subscriptionId);
}

