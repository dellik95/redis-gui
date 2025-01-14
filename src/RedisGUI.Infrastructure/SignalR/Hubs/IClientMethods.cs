using System.Threading.Tasks;
using RedisGUI.Application.RedisMetrics.Queries.GetRedisMetrics;

namespace RedisGUI.Infrastructure.SignalR.Hubs
{
	public interface IClientMethods
	{
		Task NotifyUserAsync(GetRedisMetricsResponse metrics);
	}
}
