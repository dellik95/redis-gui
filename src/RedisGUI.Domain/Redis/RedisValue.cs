using RedisGUI.Domain.Primitives;
using System;

namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents a Redis key-value pair with metadata
/// </summary>
public sealed class RedisValue
{
    /// <summary>
    /// Creates a new instance of RedisValue
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="value">Redis value</param>
    /// <param name="type">Redis data type</param>
    /// <param name="timeToLive">Time to live</param>
    private RedisValue(string key, string value, string type, TimeSpan? timeToLive)
    {
        Key = key;
        Value = value;
        Type = type;
        TimeToLive = timeToLive;
    }

    /// <summary>
    /// Redis key
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Redis value
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Redis data type
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Time to live for the key
    /// </summary>
    public TimeSpan? TimeToLive { get; }

    /// <summary>
    /// Creates a new RedisValue instance
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="value">Redis value</param>
    /// <param name="type">Redis data type</param>
    /// <param name="timeToLive">Time to live</param>
    /// <returns>Result containing the RedisValue</returns>
    public static Result<RedisValue> Create(string key, string value, string type, TimeSpan? timeToLive = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure<RedisValue>(new Error("RedisValue.EmptyKey", "Redis key cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            return Result.Failure<RedisValue>(new Error("RedisValue.EmptyType", "Redis type cannot be empty"));
        }

        return Result.Success(new RedisValue(key, value ?? string.Empty, type, timeToLive));
    }

    /// <summary>
    /// Creates a new RedisValue instance from StackExchange.Redis value
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="value">Redis value</param>
    /// <param name="type">Redis type</param>
    /// <param name="timeToLive">Time to live</param>
    /// <returns>Result containing the RedisValue</returns>
    public static Result<RedisValue> FromRedis(string key, string value, string type, TimeSpan? timeToLive = null)
    {
        return Create(key, value, type, timeToLive);
    }

    /// <summary>
    /// Returns a string that represents the current object
    /// </summary>
    /// <returns>A string representation of the RedisValue</returns>
    public override string ToString() => $"{Key}: {Value} ({Type})";
} 
