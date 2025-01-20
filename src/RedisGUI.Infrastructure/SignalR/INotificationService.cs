using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.SignalR
{
	public interface INotificationService
	{
		Task NotifyUser(string clientId, Domain.RedisMetrics.RedisMetrics metrics);
	}
}
