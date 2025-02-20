using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.SignalR.Hubs
{
	/// <summary>
	/// Defines methods that can be called from the server to connected SignalR clients
	/// </summary>
	public interface IClientMethods
	{
		/// <summary>
		/// Notifies a connected client with updated Redis metrics
		/// </summary>
		/// <param name="metrics">The Redis metrics to send to the client</param>
		Task NotifyUserAsync(Domain.RedisMetrics.RedisMetrics metrics);
	}
}
