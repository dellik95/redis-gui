using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.SignalR
{
	/// <summary>
	/// Defines operations for sending notifications to connected clients
	/// </summary>
	public interface INotificationService
	{
		/// <summary>
		/// Sends Redis metrics to a specific client
		/// </summary>
		/// <param name="clientId">The SignalR client connection ID</param>
		/// <param name="metrics">The Redis metrics to send</param>
		Task NotifyUser(string clientId, Domain.RedisMetrics.RedisMetrics metrics);
	}
}
