using RedisGUI.Domain.Audit;
using System;
using System.Collections.Generic;
using RedisGUI.Shared.Mediator;
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

/// <summary>
/// Handles the command to update a Redis value with type-specific handling
/// </summary>
internal sealed class UpdateRedisValueCommandHandler : ICommandHandler<UpdateRedisValueCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisValueService valueService;

	public UpdateRedisValueCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisValueService valueService)
	{
		this.connectionRepository = connectionRepository;
		this.valueService = valueService;
	}

	public async Task<Result> Handle(UpdateRedisValueCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(
			request.ConnectionId,
			cancellationToken);

		if (connectionResult.IsFailure)
		{
			return connectionResult;
		}

		var connection = connectionResult.Value;

		if (connection.IsReadOnly)
		{
			return Result.Failure(DomainErrors.Connection.ReadOnlyMode);
		}

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
			return Result.Failure(new Error("RedisValue.InvalidData", "Invalid value data for the specified type"));
		}

		return await valueService.UpdateValueAsync(
			connection,
			request.Key,
			valueToSet,
			request.Type.ToString(),
			request.Ttl,
			cancellationToken,
			request.Database);
	}
}
