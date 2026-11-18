using System.Text.Json.Serialization;

namespace RedisGUI.Domain.Connection;

/// <summary>A bounded scan result. A non-null cursor means further work remains.</summary>
public sealed record RedisKeyScanPage(IReadOnlyList<RedisKeyScanItem> Keys, string? NextCursor,
	[property: JsonIgnore] int MissingCount = 0, string ServerMode = "Unknown");

/// <summary>Metadata for one key, without loading its value.</summary>
public sealed record RedisKeyScanItem(string Key, string Type, double? TtlSeconds,
	[property: JsonIgnore] byte[]? RawKey = null, string? KeyBase64 = null, bool IsBinary = false);
