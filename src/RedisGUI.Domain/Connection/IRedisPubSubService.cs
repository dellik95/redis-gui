using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis Pub/Sub operations
/// </summary>
public interface IRedisPubSubService
{
	/// <summary>
	/// Publish a message to a Redis Pub/Sub channel
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="channel">Channel name</param>
	/// <param name="message">Message to publish</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> PublishMessageAsync(RedisConnection connection, string channel, string message, CancellationToken cancellationToken = default);

	/// <summary>
	/// Subscribe to a Redis Pub/Sub channel
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="channel">Channel name</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Subscription ID for managing the subscription</returns>
	Task<Result<string>> SubscribeToChannelAsync(RedisConnection connection, string channel, CancellationToken cancellationToken = default);

	/// <summary>
	/// Unsubscribe from a Redis Pub/Sub channel
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="subscriptionId">Subscription ID returned from SubscribeToChannel</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> UnsubscribeFromChannelAsync(RedisConnection connection, string subscriptionId, CancellationToken cancellationToken = default);
}
