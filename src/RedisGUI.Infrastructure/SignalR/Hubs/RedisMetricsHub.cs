using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace RedisGUI.Infrastructure.SignalR.Hubs;

public class RedisMetricsHub : Hub<IClientMethods>
{
	private readonly IRedisMetricsHubConnectionManager connectionManager;

	public RedisMetricsHub(IRedisMetricsHubConnectionManager connectionManager)
    {
        this.connectionManager = connectionManager;
    }

	public override Task OnConnectedAsync()
	{
		var httpContext = Context.GetHttpContext();
		var connectionId = httpContext?.Request.Query["connectionId"];

		if (Guid.TryParse(connectionId, out var id))
		{
			connectionManager.AddConnection(Context.ConnectionId, id);
		}

		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception exception)
    {
	    connectionManager.RemoveConnection(Context.ConnectionId);

		return base.OnConnectedAsync();
	}
} 
