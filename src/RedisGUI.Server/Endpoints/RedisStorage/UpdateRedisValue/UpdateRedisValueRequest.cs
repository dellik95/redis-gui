using System;
using RedisGUI.Application.Redis.Commands.UpdateRedisValue;

namespace RedisGUI.Server.Endpoints.RedisStorage.UpdateRedisValue;

/// <summary>
/// Request to update a Redis value with type-specific data
/// </summary>
public sealed record UpdateRedisValueRequest(
	Guid ConnectionId,
	string Key,
	RedisDataType Type,
	RedisValueData Value,
	TimeSpan? Ttl = null);
