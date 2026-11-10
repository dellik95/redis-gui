using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

/// <summary>
/// Response for import Redis data operation.
/// </summary>
/// <param name="ImportedCount">Number of keys successfully imported.</param>
/// <param name="SkippedCount">Number of keys skipped (existing keys when Overwrite=false).</param>
/// <param name="FailedCount">Number of keys that failed to import.</param>
/// <param name="FailedKeys">List of keys that failed to import.</param>
public sealed record ImportRedisDataResponse(
    int ImportedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<string> FailedKeys);
