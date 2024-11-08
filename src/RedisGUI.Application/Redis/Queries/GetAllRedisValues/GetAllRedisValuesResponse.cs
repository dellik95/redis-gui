using System;
using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Queries.GetAllRedisValues;

/// <summary>
/// Represents the response for getting all Redis values with pagination information.
/// </summary>
/// <param name="Values">Collection of Redis key-value pairs.</param>
/// <param name="TotalCount">Total number of records.</param>
/// <param name="PageNumber">Current page number.</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record GetAllRedisValuesResponse(
    IEnumerable<RedisKeyValuePair> Values,
    int TotalCount,
    int PageNumber,
    int PageSize);

/// <summary>
/// Represents a Redis key-value pair with its metadata.
/// </summary>
/// <param name="Key">The Redis key.</param>
/// <param name="Value">The Redis value.</param>
/// <param name="Type">The Redis data type.</param>
/// <param name="TimeToLive">Time until the key expires (null if no expiration).</param>
public sealed record RedisKeyValuePair(
    string Key,
    string Value,
    string Type,
    TimeSpan? TimeToLive); 