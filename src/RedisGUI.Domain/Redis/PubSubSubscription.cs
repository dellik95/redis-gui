using System;

namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents a Pub/Sub subscription
/// </summary>
public sealed class PubSubSubscription
{
	/// <summary>
	/// Unique subscription identifier
	/// </summary>
	public string SubscriptionId { get; init; } = string.Empty;

	/// <summary>
	/// Channel name
	/// </summary>
	public string Channel { get; init; } = string.Empty;

	/// <summary>
	/// Connection ID
	/// </summary>
	public Guid ConnectionId { get; init; }

	/// <summary>
	/// Creation timestamp
	/// </summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Number of messages received
	/// </summary>
	public long MessagesReceived { get; set; }

	private PubSubSubscription() { }

	/// <summary>
	/// Create a new subscription
	/// </summary>
	public static PubSubSubscription Create(Guid connectionId, string channel)
	{
		return new PubSubSubscription
		{
			SubscriptionId = Guid.NewGuid().ToString(),
			ConnectionId = connectionId,
			Channel = channel,
			CreatedAt = DateTime.UtcNow,
			MessagesReceived = 0
		};
	}
}
