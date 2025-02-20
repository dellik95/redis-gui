using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Service for sending notifications to connected SignalR clients
/// </summary>
public class NotificationService : INotificationService
{
	private readonly IHubContext<RedisMetricsHub, IClientMethods> hubContext;

	/// <summary>
	/// Initializes a new instance of the NotificationService
	/// </summary>
	/// <param name="hubContext">The SignalR hub context for sending messages</param>
	public NotificationService(IHubContext<RedisMetricsHub, IClientMethods> hubContext)
	{
		this.hubContext = hubContext;
	}

	/// <summary>
	/// Sends Redis metrics to a specific client
	/// </summary>
	/// <param name="clientId">The SignalR client connection ID</param>
	/// <param name="metrics">The Redis metrics to send</param>
	public async Task NotifyUser(string clientId, Domain.RedisMetrics.RedisMetrics metrics)
	{
		await hubContext.Clients.Client(clientId).NotifyUserAsync(metrics);
	}
}
