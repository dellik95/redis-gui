using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Service for sending key change notifications to clients
/// </summary>
public interface IKeyChangeNotificationService
{
	/// <summary>
	/// Notifies clients watching a Redis connection about a key change
	/// </summary>
	/// <param name="redisConnectionId">The Redis connection ID</param>
	/// <param name="changeEvent">The key change event</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task NotifyKeyChange(string redisConnectionId, KeyChangeEventDto changeEvent, CancellationToken cancellationToken = default);
}

