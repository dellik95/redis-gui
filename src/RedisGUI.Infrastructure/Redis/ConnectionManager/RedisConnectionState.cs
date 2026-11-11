namespace RedisGUI.Infrastructure.Redis.ConnectionManager;

/// <summary>
/// Represents the lifecycle state of a managed Redis connection.
/// </summary>
public enum RedisConnectionState
{
	/// <summary>
	/// The connection is currently being initialized.
	/// </summary>
	Connecting,

	/// <summary>
	/// The connection is established and healthy.
	/// </summary>
	Connected,

	/// <summary>
	/// The connection was lost and is actively attempting automatic reconnection.
	/// </summary>
	Reconnecting,

	/// <summary>
	/// The connection is currently disconnected from the Redis server.
	/// </summary>
	Disconnected,

	/// <summary>
	/// The connection initialization failed or encountered a permanent fault.
	/// </summary>
	Faulted,

	/// <summary>
	/// The connection is in the process of being retired or disposed.
	/// </summary>
	Disposing
}
