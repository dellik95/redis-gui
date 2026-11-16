namespace RedisGUI.Domain.Connection;

/// <summary>A bounded scan result. A non-null cursor means further work remains.</summary>
public sealed record RedisKeyScanPage(IReadOnlyList<RedisKeyScanItem> Keys, string? NextCursor);

/// <summary>Metadata for one key, without loading its value.</summary>
public sealed record RedisKeyScanItem(string Key, string Type, double? TtlSeconds);
