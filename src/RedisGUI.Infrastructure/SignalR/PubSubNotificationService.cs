using Microsoft.AspNetCore.SignalR;
using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Implementation of Pub/Sub notification service using SignalR
/// </summary>
public sealed class PubSubNotificationService : IPubSubNotificationService
{
	private readonly IHubContext<PubSubHub, IPubSubClientMethods> _hubContext;

	/// <summary>
	/// Initializes a new instance of PubSubNotificationService
	/// </summary>
	/// <param name="hubContext">The SignalR hub context</param>
	public PubSubNotificationService(IHubContext<PubSubHub, IPubSubClientMethods> hubContext)
	{
		_hubContext = hubContext;
	}

	/// <inheritdoc />
	public async Task BroadcastMessage(string subscriptionId, PubSubMessageDto message, CancellationToken cancellationToken = default)
	{
		await _hubContext.Clients.Group(subscriptionId).ReceiveMessage(message);
	}
}

