using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace RedisGUI.Infrastructure.SignalR.Hubs;

/// <summary>
/// SignalR hub for broadcasting Redis metrics to connected clients
/// </summary>
public class RedisMetricsHub : Hub<IClientMethods>
{
	private readonly IHubSubscribersManager hubSubscribersManager;

	/// <summary>
	/// Initializes a new instance of the RedisMetricsHub
	/// </summary>
	/// <param name="hubSubscribersManager">Service to manage hub subscriber connections</param>
	public RedisMetricsHub(IHubSubscribersManager hubSubscribersManager)
	{
		this.hubSubscribersManager = hubSubscribersManager;
	}

	/// <summary>
	/// Handles new client connections by registering them with the subscriber manager
	/// </summary>
	public override Task OnConnectedAsync()
	{
		var httpContext = Context.GetHttpContext();
		var connectionId = httpContext?.Request.Query["connectionId"];

		if (Guid.TryParse(connectionId, out var id))
		{
			hubSubscribersManager.AddConnection(Context.ConnectionId, id);
		}

		return base.OnConnectedAsync();
	}

	/// <summary>
	/// Handles client disconnections by removing them from the subscriber manager
	/// </summary>
	/// <param name="exception">Exception that caused the disconnection, if any</param>
	public override Task OnDisconnectedAsync(Exception exception)
	{
		hubSubscribersManager.RemoveConnection(Context.ConnectionId);

		return base.OnConnectedAsync();
	}
}
