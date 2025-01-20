using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.SignalR;

public class NotificationService : INotificationService
{
	private readonly IHubContext<RedisMetricsHub, IClientMethods> hubContext;

	public NotificationService(IHubContext<RedisMetricsHub, IClientMethods> hubContext)
	{
		this.hubContext = hubContext;
	}

	public async Task NotifyUser(string clientId, Domain.RedisMetrics.RedisMetrics metrics)
	{
		await hubContext.Clients.Client(clientId).NotifyUserAsync(metrics);
	}
}
