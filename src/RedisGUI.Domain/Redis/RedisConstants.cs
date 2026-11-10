namespace RedisGUI.Domain.Redis;

/// <summary>
/// Constants for Redis configuration and validation
/// </summary>
public static class RedisConstants
{
	/// <summary>
	/// Minimum valid port number
	/// </summary>
	public const int MinPort = 1;

	/// <summary>
	/// Maximum valid port number
	/// </summary>
	public const int MaxPort = 65535;

	/// <summary>
	/// Default Redis port
	/// </summary>
	public const int DefaultPort = 6379;

	/// <summary>
	/// Maximum Redis key length
	/// </summary>
	public const int MaxKeyLength = 512;

	/// <summary>
	/// Default database number
	/// </summary>
	public const int DefaultDatabase = 0;

	/// <summary>
	/// Maximum number of databases in Redis
	/// </summary>
	public const int MaxDatabases = 16;

	/// <summary>
	/// Default connection timeout in seconds
	/// </summary>
	public const int DefaultConnectionTimeoutSeconds = 30;

	/// <summary>
	/// Maximum transaction operations allowed
	/// </summary>
	public const int MaxTransactionOperations = 100;

	/// <summary>
	/// Maximum data import size in megabytes
	/// </summary>
	public const int MaxImportSizeMB = 10;

	/// <summary>
	/// Maximum export pattern length
	/// </summary>
	public const int MaxPatternLength = 256;
}
