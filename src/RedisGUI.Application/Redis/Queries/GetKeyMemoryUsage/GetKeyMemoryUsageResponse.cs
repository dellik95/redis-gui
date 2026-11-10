namespace RedisGUI.Application.Redis.Queries.GetKeyMemoryUsage;

/// <summary>
/// Response containing memory usage of a Redis key
/// </summary>
/// <param name="Key">The Redis key name</param>
/// <param name="MemoryBytes">Memory usage in bytes</param>
public sealed record GetKeyMemoryUsageResponse(
    string Key,
    long MemoryBytes);
