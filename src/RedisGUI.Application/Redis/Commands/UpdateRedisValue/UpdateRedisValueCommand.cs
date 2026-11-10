using System;
using System.Collections.Generic;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.UpdateRedisValue;

/// <summary>
/// Command to update a Redis value with type-specific handling.
/// </summary>
public sealed record UpdateRedisValueCommand(
    Guid ConnectionId,
    string Key,
    RedisDataType Type,
    RedisValueData Value,
    TimeSpan? Ttl = null,
    int? Database = null) : ICommand;

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
/// Container class for Redis value data across all types.
/// </summary>
public class RedisValueData
{
    public string? Value { get; set; }
    public List<string>? Items { get; set; }
    public HashSet<string>? Members { get; set; }
    public Dictionary<string, string>? Fields { get; set; }
    public Dictionary<string, double>? MembersWithScores { get; set; }
    public List<StreamEntry>? Entries { get; set; }

    public RedisValueData() { }
    public RedisValueData(string? value) => Value = value;
}

public sealed class StringValueData : RedisValueData
{
    public StringValueData() { }
    public StringValueData(string value) : base(value) { }
}

public sealed class ListValueData : RedisValueData
{
    public ListValueData() { }
    public ListValueData(List<string> items) { Items = items; }
}

public sealed class SetValueData : RedisValueData
{
    public SetValueData() { }
    public SetValueData(HashSet<string> members) { Members = members; }
}

public sealed class HashValueData : RedisValueData
{
    public HashValueData() { }
    public HashValueData(Dictionary<string, string> fields) { Fields = fields; }
}

public sealed class SortedSetValueData : RedisValueData
{
    public SortedSetValueData() { }
    public SortedSetValueData(Dictionary<string, double> membersWithScores) { MembersWithScores = membersWithScores; }
}

public sealed class StreamValueData : RedisValueData
{
    public StreamValueData() { }
    public StreamValueData(List<StreamEntry> entries) { Entries = entries; }
}

public sealed record StreamEntry(string Id, Dictionary<string, string> Fields);
