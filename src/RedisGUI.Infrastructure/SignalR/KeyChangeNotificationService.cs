using Microsoft.AspNetCore.SignalR;
using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Implementation of key change notification service using SignalR
/// </summary>
public sealed class KeyChangeNotificationService : IKeyChangeNotificationService
{
	private readonly IHubContext<KeyChangeHub, IKeyChangeClientMethods> _hubContext;

	/// <summary>
	/// Initializes a new instance of KeyChangeNotificationService
	/// </summary>
	/// <param name="hubContext">The SignalR hub context</param>
	public KeyChangeNotificationService(IHubContext<KeyChangeHub, IKeyChangeClientMethods> hubContext)
	{
		_hubContext = hubContext;
	}

	/// <inheritdoc />
	public async Task NotifyKeyChange(string redisConnectionId, KeyChangeEventDto changeEvent, CancellationToken cancellationToken = default)
	{
		await _hubContext.Clients.Group($"keychange:{redisConnectionId}").KeyChanged(changeEvent);
	}
}

