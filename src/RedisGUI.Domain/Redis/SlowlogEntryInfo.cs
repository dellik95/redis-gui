using System;

namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents an entry from Redis SLOWLOG
/// </summary>
public sealed record SlowlogEntryInfo(
	long Id,
	DateTime Timestamp,
	TimeSpan Duration,
	string[] Arguments,
	string ClientAddress,
	string ClientName
);
