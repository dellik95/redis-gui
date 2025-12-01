using System;
using System.Collections.Generic;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.UpdateRedisValue;

/// <summary>
/// Command to update a Redis value with type-specific handling.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Key">The key to update.</param>
/// <param name="Type">The Redis data type (String, List, Hash, Set, SortedSet, Stream).</param>
/// <param name="Value">The value(s) to set - format depends on type.</param>
/// <param name="Ttl">Optional time-to-live for the key.</param>
public sealed record UpdateRedisValueCommand(
    Guid ConnectionId,
    string Key,
    RedisDataType Type,
    RedisValueData Value,
    TimeSpan? Ttl = null) : ICommand;

/// <summary>
/// Redis data types.
/// </summary>
public enum RedisDataType
{
    String,
    List,
    Set,
    Hash,
    SortedSet,
    Stream
}

/// <summary>
/// Base class for Redis value data.
/// </summary>
public abstract record RedisValueData;

/// <summary>
/// String value data.
/// </summary>
public sealed record StringValueData(string Value) : RedisValueData;

/// <summary>
/// List value data.
/// </summary>
public sealed record ListValueData(List<string> Items) : RedisValueData;

/// <summary>
/// Set value data.
/// </summary>
public sealed record SetValueData(HashSet<string> Members) : RedisValueData;

/// <summary>
/// Hash value data.
/// </summary>
public sealed record HashValueData(Dictionary<string, string> Fields) : RedisValueData;

/// <summary>
/// Sorted Set value data.
/// </summary>
public sealed record SortedSetValueData(Dictionary<string, double> MembersWithScores) : RedisValueData;

/// <summary>
/// Stream value data.
/// </summary>
public sealed record StreamValueData(List<StreamEntry> Entries) : RedisValueData;

/// <summary>
/// Stream entry.
/// </summary>
public sealed record StreamEntry(string Id, Dictionary<string, string> Fields);
