using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace RedisGUI.Infrastructure.SignalR.Hubs;

public class RedisMetricsHub : Hub<IClientMethods>
{
	private readonly IHubSubscribersManager hubSubscribersManager;

	public RedisMetricsHub(IHubSubscribersManager hubSubscribersManager)
	{
		this.hubSubscribersManager = hubSubscribersManager;
	}

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

	public override Task OnDisconnectedAsync(Exception exception)
	{
		hubSubscribersManager.RemoveConnection(Context.ConnectionId);

		return base.OnConnectedAsync();
	}
}
