namespace RedisGUI.Application.Redis.Queries.SubscribeToChannel;

/// <summary>
/// Response for subscribing to a Redis channel.
/// </summary>
/// <param name="SubscriptionId">Unique identifier for this subscription.</param>
/// <param name="Channel">The channel that was subscribed to.</param>
/// <param name="Success">Indicates if subscription was successful.</param>
public sealed record SubscribeToChannelResponse(
	string SubscriptionId,
	string Channel,
	bool Success);
