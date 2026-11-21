using FluentResults;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using System;
using System.Collections.Generic;
using DispatchWeave;
using DispatchWeave.Results;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Primitives;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.UpdateRedisValue;

/// <summary>
/// Command to update a Redis value with type-specific handling.
/// </summary>
[Auditable(AuditAction.Update, "Redis")]
public sealed record UpdateRedisValueCommand(
	Guid ConnectionId,
	string Key,
	RedisDataType Type,
	RedisValueData Value,
	TimeSpan? Ttl = null,
	int? Database = null,
	string? KeyBase64 = null) : ICommand, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

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

/// <summary>
/// Handles the command to update a Redis value with type-specific handling
/// </summary>
internal sealed class UpdateRedisValueCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisValueService valueService,
	IConnectionAccessService? access = null) : ICommandHandler<UpdateRedisValueCommand>
{
	public async Task<Result> Handle(UpdateRedisValueCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);

		if (connectionResult.IsFailed)
		{
			return connectionResult.ToResult();
		}

		var connection = connectionResult.Value;

		object? valueToSet = request.Type switch
		{
			RedisDataType.String => request.Value.Value ?? string.Empty,
			RedisDataType.List => (object?)request.Value.Items ?? new List<string>(),
			RedisDataType.Set => (object?)request.Value.Members ?? new HashSet<string>(),
			RedisDataType.Hash => (object?)request.Value.Fields ?? new Dictionary<string, string>(),
			RedisDataType.SortedSet => (object?)request.Value.MembersWithScores ?? new Dictionary<string, double>(),
			RedisDataType.Stream => (object?)request.Value.Entries?.Select(e => e.Fields).ToList() ?? new List<Dictionary<string, string>>(),
			_ => null
		};

		if (valueToSet == null)
		{
			return Result.Fail(new DomainError("RedisValue.InvalidData", "Invalid value data for the specified type"));
		}

		return string.IsNullOrEmpty(request.KeyBase64)
			? await valueService.UpdateValueAsync(
				connection,
				request.Key,
				valueToSet,
				request.Type.ToString(),
				request.Ttl,
				cancellationToken,
				request.Database)
			: await valueService.UpdateValueByIdentityAsync(
				connection,
				request.Key,
				request.KeyBase64,
				valueToSet,
				request.Type.ToString(),
				request.Ttl,
				cancellationToken,
				request.Database);
	}
}
