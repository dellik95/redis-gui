namespace RedisGUI.Infrastructure.SignalR.Hubs;

/// <summary>
/// Defines methods for Pub/Sub client notifications
/// </summary>
public interface IPubSubClientMethods
{
	/// <summary>
	/// Receives a message from a subscribed Redis channel
	/// </summary>
	/// <param name="message">The Pub/Sub message data</param>
	Task ReceiveMessage(PubSubMessageDto message);
}

/// <summary>
/// Pub/Sub message data transfer object
/// </summary>
/// <param name="ConnectionId">The connection ID</param>
/// <param name="Channel">The channel name</param>
/// <param name="Message">The message content</param>
/// <param name="Timestamp">When the message was received</param>
public sealed record PubSubMessageDto(
	string ConnectionId,
	string Channel,
	string Message,
	string Timestamp);

