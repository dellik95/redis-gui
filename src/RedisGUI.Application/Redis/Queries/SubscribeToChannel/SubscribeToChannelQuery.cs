using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Queries.SubscribeToChannel;

/// <summary>
/// Query to subscribe to a Redis Pub/Sub channel.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Channel">The channel name to subscribe to.</param>
public sealed record SubscribeToChannelQuery(
	Guid ConnectionId,
	string Channel) : IQuery<SubscribeToChannelResponse>;
