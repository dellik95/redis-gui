using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.SignalR.Hubs
{
	public interface IClientMethods
	{
		Task NotifyUserAsync(Domain.RedisMetrics.RedisMetrics metrics);
	}
}
