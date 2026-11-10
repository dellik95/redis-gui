namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents a section of Redis INFO output with key-value properties
/// </summary>
public sealed record RedisServerInfoSection(string Name, Dictionary<string, string> Properties);

/// <summary>
/// Data transfer object containing structured Redis server statistics and telemetry
/// </summary>
public sealed record RedisServerInfoDto(
	string RedisVersion,
	string RedisMode,
	string Os,
	long UptimeInSeconds,
	long ConnectedClients,
	long UsedMemoryBytes,
	long TotalSystemMemoryBytes,
	double MemoryFragmentationRatio,
	long TotalCommandsProcessed,
	long KeyspaceHits,
	long KeyspaceMisses,
	double HitRate,
	List<RedisServerInfoSection> AllSections);

/// <summary>
/// Represents a Redis configuration parameter
/// </summary>
public sealed record RedisConfigParameterDto(string Name, string Value, bool IsModifiable);

/// <summary>
/// Data transfer object containing Redis server configuration parameters
/// </summary>
public sealed record RedisServerConfigDto(List<RedisConfigParameterDto> Parameters, int TotalParameters);

/// <summary>
/// Response data transfer object for configuration set operations
/// </summary>
public sealed record RedisSetConfigResponseDto(bool Success, string Message, string ParameterName, string NewValue);
