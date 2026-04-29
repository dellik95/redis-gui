using Microsoft.AspNetCore.SignalR;

namespace RedisGUI.Infrastructure.SignalR.Hubs;

/// <summary>
/// SignalR hub for Redis Pub/Sub messaging
/// Allows clients to receive real-time messages from subscribed Redis channels
/// </summary>
public sealed class PubSubHub : Hub<IPubSubClientMethods>
{
	private readonly IPubSubSubscriptionManager _subscriptionManager;

	/// <summary>
	/// Initializes a new instance of PubSubHub
	/// </summary>
	/// <param name="subscriptionManager">Service to manage Pub/Sub subscriptions</param>
	public PubSubHub(IPubSubSubscriptionManager subscriptionManager)
	{
		_subscriptionManager = subscriptionManager;
	}

	/// <summary>
	/// Handles new client connections
	/// </summary>
	public override Task OnConnectedAsync()
	{
		return base.OnConnectedAsync();
	}

	/// <summary>
	/// Handles client disconnections by cleaning up subscriptions
	/// </summary>
	/// <param name="exception">Exception that caused the disconnection, if any</param>
	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		await _subscriptionManager.RemoveClientSubscriptions(Context.ConnectionId, Context.ConnectionAborted);
		await base.OnDisconnectedAsync(exception);
	}

	/// <summary>
	/// Joins a client to a subscription group for receiving channel messages
	/// </summary>
	/// <param name="subscriptionId">The subscription identifier</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public async Task JoinSubscription(string subscriptionId, CancellationToken cancellationToken = default)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, subscriptionId, cancellationToken);
		_subscriptionManager.AddClientToSubscription(Context.ConnectionId, subscriptionId);
	}

	/// <summary>
	/// Leaves a subscription group
	/// </summary>
	/// <param name="subscriptionId">The subscription identifier</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public async Task LeaveSubscription(string subscriptionId, CancellationToken cancellationToken = default)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, subscriptionId, cancellationToken);
		_subscriptionManager.RemoveClientFromSubscription(Context.ConnectionId, subscriptionId);
	}
}

