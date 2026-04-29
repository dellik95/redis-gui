using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace RedisGUI.Infrastructure.SignalR.Hubs;

/// <summary>
/// SignalR hub for Redis key change notifications
/// Allows clients to receive real-time notifications when keys are modified
/// </summary>
public sealed class KeyChangeHub : Hub<IKeyChangeClientMethods>
{
	private readonly IKeyChangeSubscriptionManager _subscriptionManager;

	/// <summary>
	/// Initializes a new instance of KeyChangeHub
	/// </summary>
	/// <param name="subscriptionManager">Service to manage key change subscriptions</param>
	public KeyChangeHub(IKeyChangeSubscriptionManager subscriptionManager)
	{
		_subscriptionManager = subscriptionManager;
	}

	/// <summary>
	/// Handles new client connections
	/// </summary>
	public override Task OnConnectedAsync()
	{
		HttpContext? httpContext = Context.GetHttpContext();
		string? connectionId = httpContext?.Request.Query["connectionId"].ToString();

		if (!string.IsNullOrEmpty(connectionId))
		{
			_subscriptionManager.SubscribeToConnection(Context.ConnectionId, connectionId);
		}

		return base.OnConnectedAsync();
	}

	/// <summary>
	/// Handles client disconnections
	/// </summary>
	/// <param name="exception">Exception that caused the disconnection, if any</param>
	public override Task OnDisconnectedAsync(Exception? exception)
	{
		_subscriptionManager.UnsubscribeClient(Context.ConnectionId);
		return base.OnDisconnectedAsync(exception);
	}

	/// <summary>
	/// Subscribes to key changes for a specific Redis connection
	/// </summary>
	/// <param name="redisConnectionId">The Redis connection ID to watch</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public async Task WatchConnection(string redisConnectionId, CancellationToken cancellationToken = default)
	{
		_subscriptionManager.SubscribeToConnection(Context.ConnectionId, redisConnectionId);
		await Groups.AddToGroupAsync(Context.ConnectionId, $"keychange:{redisConnectionId}", cancellationToken);
	}

	/// <summary>
	/// Unsubscribes from key changes for a specific Redis connection
	/// </summary>
	/// <param name="redisConnectionId">The Redis connection ID to stop watching</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public async Task UnwatchConnection(string redisConnectionId, CancellationToken cancellationToken = default)
	{
		_subscriptionManager.UnsubscribeFromConnection(Context.ConnectionId, redisConnectionId);
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"keychange:{redisConnectionId}", cancellationToken);
	}
}

