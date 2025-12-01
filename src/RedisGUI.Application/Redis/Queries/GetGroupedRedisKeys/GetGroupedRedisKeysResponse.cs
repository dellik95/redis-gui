using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Queries.GetGroupedRedisKeys;

/// <summary>
/// Response containing grouped Redis keys.
/// </summary>
/// <param name="Groups">Collection of key groups.</param>
/// <param name="TotalKeys">Total number of keys.</param>
/// <param name="Delimiter">The delimiter used for grouping.</param>
/// <param name="ViewMode">The view mode (flat or tree).</param>
public sealed record GetGroupedRedisKeysResponse(
    IEnumerable<RedisKeyGroup> Groups,
    int TotalKeys,
    string Delimiter,
    string ViewMode);

/// <summary>
/// Represents a group of Redis keys.
/// </summary>
public sealed record RedisKeyGroup
{
    /// <summary>
    /// Group name (prefix before delimiter).
    /// </summary>
    public string GroupName { get; init; } = string.Empty;

    /// <summary>
    /// Full path to this group.
    /// </summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>
    /// Keys in this group.
    /// </summary>
    public IEnumerable<RedisKeyInfo> Keys { get; init; } = [];

    /// <summary>
    /// Child groups (for tree mode).
    /// </summary>
    public IEnumerable<RedisKeyGroup> Children { get; init; } = [];

    /// <summary>
    /// Number of keys in this group (including children).
    /// </summary>
    public int KeyCount { get; init; }

    /// <summary>
    /// Whether this group is expanded (UI state).
    /// </summary>
    public bool IsExpanded { get; init; }
}

/// <summary>
/// Represents information about a Redis key.
/// </summary>
public sealed record RedisKeyInfo
{
    /// <summary>
    /// The Redis key.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Short name (part after last delimiter).
    /// </summary>
    public string ShortName { get; init; } = string.Empty;

    /// <summary>
    /// Redis data type (String, List, Set, Hash, SortedSet, Stream).
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Size/length of the value.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Time to live in seconds (null if no expiration).
    /// </summary>
    public long? TtlSeconds { get; init; }

    /// <summary>
    /// Memory usage in bytes.
    /// </summary>
    public long? MemoryUsage { get; init; }
}
