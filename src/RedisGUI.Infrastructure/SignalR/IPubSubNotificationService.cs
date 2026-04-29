using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Service for sending Pub/Sub notifications to clients
/// </summary>
public interface IPubSubNotificationService
{
	/// <summary>
	/// Broadcasts a message to all clients subscribed to a subscription
	/// </summary>
	/// <param name="subscriptionId">The subscription identifier</param>
	/// <param name="message">The message to broadcast</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task BroadcastMessage(string subscriptionId, PubSubMessageDto message, CancellationToken cancellationToken = default);
}

