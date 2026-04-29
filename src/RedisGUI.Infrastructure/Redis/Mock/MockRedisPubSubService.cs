using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.SignalR;
using RedisGUI.Infrastructure.SignalR.Hubs;
using RedisGUI.Shared.Primitives;
using System.Collections.Concurrent;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Mock implementation of IRedisPubSubService for testing
/// </summary>
public sealed class MockRedisPubSubService : IRedisPubSubService
{
	private readonly IPubSubNotificationService? _pubSubNotificationService;
	private readonly ILogger<MockRedisPubSubService>? _logger;
	private readonly ConcurrentDictionary<string, PubSubSubscription> _subscriptions = new();
	private readonly ConcurrentDictionary<string, List<string>> _pubSubChannels = new();

	public MockRedisPubSubService(
		IPubSubNotificationService? pubSubNotificationService = null,
		ILogger<MockRedisPubSubService>? logger = null)
	{
		_pubSubNotificationService = pubSubNotificationService;
		_logger = logger;
	}

	public async Task<Result> PublishMessageAsync(RedisConnection connection, string channel, string message, CancellationToken cancellationToken = default)
	{
		try
		{
			// Store message in channel
			if (!_pubSubChannels.ContainsKey(channel))
			{
				_pubSubChannels[channel] = [];
			}
			_pubSubChannels[channel].Add(message);

			// Broadcast to all subscribers via SignalR
			int subscribersCount = 0;
			foreach (var subscription in _subscriptions.Values.Where(s => s.Channel == channel))
			{
				subscribersCount++;
				if (_pubSubNotificationService != null)
				{
					PubSubMessageDto messageDto = new(
						ConnectionId: subscription.ConnectionId.ToString(),
						Channel: channel,
						Message: message,
						Timestamp: DateTime.UtcNow.ToString("O"));

					try
					{
						await _pubSubNotificationService.BroadcastMessage(subscription.SubscriptionId, messageDto, cancellationToken);
					}
					catch (Exception ex)
					{
						_logger?.LogError(ex, "Failed to broadcast Pub/Sub message via SignalR for subscription {SubscriptionId}", subscription.SubscriptionId);
					}
				}
			}

			_logger?.LogInformation("Published message to channel {Channel}, received by {Recipients} subscribers", channel, subscribersCount);
			return Result.Success();
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error publishing message to channel {Channel}", channel);
			return Result.Failure(DomainErrors.Redis.PublishFailed);
		}
	}

	public Task<Result<string>> SubscribeToChannelAsync(RedisConnection connection, string channel, CancellationToken cancellationToken = default)
	{
		try
		{
			PubSubSubscription subscription = PubSubSubscription.Create(connection.Id, channel);
			_subscriptions.TryAdd(subscription.SubscriptionId, subscription);

			if (!_pubSubChannels.ContainsKey(channel))
			{
				_pubSubChannels[channel] = [];
			}

			_logger?.LogInformation("Subscribed to channel {Channel} with ID {SubscriptionId}", channel, subscription.SubscriptionId);
			return Task.FromResult(Result.Success(subscription.SubscriptionId));
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error subscribing to channel {Channel}", channel);
			return Task.FromResult(Result.Failure<string>(DomainErrors.Redis.SubscribeFailed));
		}
	}

	public Task<Result> UnsubscribeFromChannelAsync(RedisConnection connection, string subscriptionId, CancellationToken cancellationToken = default)
	{
		try
		{
			if (!_subscriptions.TryRemove(subscriptionId, out PubSubSubscription? subscription))
			{
				return Task.FromResult(Result.Failure(DomainErrors.Redis.SubscriptionNotFound));
			}

			_logger?.LogInformation("Unsubscribed from channel {Channel}", subscription.Channel);
			return Task.FromResult(Result.Success());
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error unsubscribing from subscription {SubscriptionId}", subscriptionId);
			return Task.FromResult(Result.Failure(DomainErrors.Redis.UnsubscribeFailed));
		}
	}
}
