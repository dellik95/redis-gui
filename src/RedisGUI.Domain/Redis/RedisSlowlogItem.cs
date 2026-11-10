using System;

namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents an entry from Redis SLOWLOG
/// </summary>
/// <param name="Id">Slowlog unique entry identifier</param>
/// <param name="Timestamp">UTC execution timestamp</param>
/// <param name="DurationMicroseconds">Execution duration in microseconds</param>
/// <param name="Command">Executed command line</param>
/// <param name="ClientAddress">Optional client address</param>
public sealed record RedisSlowlogItem(
    long Id,
    DateTime Timestamp,
    long DurationMicroseconds,
    string Command,
    string ClientAddress = "");
