using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;

/// <summary>
/// Response for bulk delete multiple keys operation.
/// </summary>
/// <param name="DeletedCount">Number of keys successfully deleted.</param>
/// <param name="TotalRequested">Total number of keys requested for deletion.</param>
/// <param name="FailedKeys">List of keys that failed to delete.</param>
public sealed record DeleteMultipleKeysResponse(
    int DeletedCount,
    int TotalRequested,
    IReadOnlyList<string> FailedKeys);
