using System.Threading.Tasks;
using RedisGUI.Application.RedisMetrics.Queries.GetRedisMetrics;

namespace RedisGUI.Infrastructure.SignalR
{
	public interface INotificationService
	{
		Task NotifyUser(string clientId, GetRedisMetricsResponse metrics);
	}
}
