using Microsoft.Extensions.Logging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.SignalR;
using RedisGUI.Infrastructure.SignalR.Hubs;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis Pub/Sub operations
/// </summary>
public class RedisPubSubService : IRedisPubSubService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<RedisPubSubService> logger;
	private readonly IPubSubNotificationService? pubSubNotificationService;
	private readonly ConcurrentDictionary<string, (ISubscriber Subscriber, PubSubSubscription Subscription)> subscriptions = new();

	public RedisPubSubService(
		IConnectionPool connectionPool,
		ILogger<RedisPubSubService> logger,
		IPubSubNotificationService? pubSubNotificationService = null)
	{
		this.connectionPool = connectionPool;
		this.logger = logger;
		this.pubSubNotificationService = pubSubNotificationService;
	}

	/// <summary>
	/// Publishes a message to a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result> PublishMessageAsync(RedisConnection connection, string channel, string message, CancellationToken cancellationToken = default)
	{
		try
		{
			Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure(multiplexerResult.Error);
			}

			ConnectionMultiplexer multiplexer = multiplexerResult.Value;
			ISubscriber subscriber = multiplexer.GetSubscriber();

			long recipients = await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
			logger.LogInformation("Published message to channel {Channel}, received by {Recipients} subscribers", channel, recipients);

			return Result.Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error publishing message to channel {Channel}", channel);
			return Result.Failure(DomainErrors.Redis.PublishFailed);
		}
	}

	/// <summary>
	/// Subscribes to a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result<string>> SubscribeToChannelAsync(
		RedisConnection connection,
		string channel,
		CancellationToken cancellationToken = default)
	{
		try
		{
			Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure<string>(multiplexerResult.Error);
			}

			ConnectionMultiplexer multiplexer = multiplexerResult.Value;
			ISubscriber subscriber = multiplexer.GetSubscriber();

			PubSubSubscription subscription = PubSubSubscription.Create(connection.Id, channel);

			await subscriber.SubscribeAsync(RedisChannel.Literal(channel), async (ch, message) =>
			{
				logger.LogInformation("Received message on channel {Channel}: {Message}", ch, message);
				subscription.MessagesReceived++;

				// Broadcast via SignalR if notification service is available
				if (pubSubNotificationService != null)
				{
					PubSubMessageDto messageDto = new(
						ConnectionId: connection.Id.ToString(),
						Channel: ch.ToString(),
						Message: message.ToString(),
						Timestamp: DateTime.UtcNow.ToString("O"));

					try
					{
						await pubSubNotificationService.BroadcastMessage(subscription.SubscriptionId, messageDto);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Failed to broadcast Pub/Sub message via SignalR for subscription {SubscriptionId}", subscription.SubscriptionId);
					}
				}
			});

			subscriptions.TryAdd(subscription.SubscriptionId, (subscriber, subscription));

			logger.LogInformation("Subscribed to channel {Channel} with ID {SubscriptionId}", channel, subscription.SubscriptionId);
			return Result.Success(subscription.SubscriptionId);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error subscribing to channel {Channel}", channel);
			return Result.Failure<string>(DomainErrors.Redis.SubscribeFailed);
		}
	}

	/// <summary>
	/// Unsubscribes from a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result> UnsubscribeFromChannelAsync(
		RedisConnection connection,
		string subscriptionId,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (!subscriptions.TryRemove(subscriptionId, out (ISubscriber Subscriber, PubSubSubscription Subscription) sub))
			{
				return Result.Failure(DomainErrors.Redis.SubscriptionNotFound);
			}

			await sub.Subscriber.UnsubscribeAsync(RedisChannel.Literal(sub.Subscription.Channel));
			logger.LogInformation("Unsubscribed from channel {Channel}", sub.Subscription.Channel);

			return Result.Success();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error unsubscribing from subscription {SubscriptionId}", subscriptionId);
			return Result.Failure(DomainErrors.Redis.UnsubscribeFailed);
		}
	}
}
