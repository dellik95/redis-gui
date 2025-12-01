using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.UnsubscribeFromChannel;

/// <summary>
/// Command to unsubscribe from a Redis Pub/Sub channel.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="SubscriptionId">The subscription ID to cancel.</param>
public sealed record UnsubscribeFromChannelCommand(
	Guid ConnectionId,
	string SubscriptionId) : ICommand;
