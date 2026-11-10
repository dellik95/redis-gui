using System;
using System.Text.Json.Serialization;
using RedisGUI.Application.Redis.Commands.UpdateRedisValue;

namespace RedisGUI.Server.Endpoints.RedisStorage.UpdateRedisValue;

/// <summary>
/// Request to update a Redis value with type-specific data
/// </summary>
public sealed class UpdateRedisValueRequest
{
	public Guid ConnectionId { get; set; }
	public string Key { get; set; } = string.Empty;

	[JsonConverter(typeof(JsonStringEnumConverter))]
	public RedisDataType Type { get; set; }

	public RedisValueData Value { get; set; } = new();
	public double? Ttl { get; set; }
	public int? Database { get; set; }
}
