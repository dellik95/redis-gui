namespace RedisGUI.Infrastructure.SignalR.Hubs;

/// <summary>
/// Defines methods for key change client notifications
/// </summary>
public interface IKeyChangeClientMethods
{
	/// <summary>
	/// Receives a key change notification
	/// </summary>
	/// <param name="changeEvent">The key change event data</param>
	Task KeyChanged(KeyChangeEventDto changeEvent);
}

/// <summary>
/// Key change event data transfer object
/// </summary>
/// <param name="ConnectionId">The connection ID</param>
/// <param name="Key">The Redis key that changed</param>
/// <param name="Type">The key type (string, hash, list, etc.)</param>
/// <param name="Action">The action performed (created, updated, deleted)</param>
/// <param name="Timestamp">When the change occurred</param>
public sealed record KeyChangeEventDto(
	string ConnectionId,
	string Key,
	string Type,
	string Action,
	string Timestamp);

