using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.SeedSampleData;

/// <summary>Runs bounded sample-data seeding as a cancellable background job.</summary>
public sealed class SeedSampleDataJobHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IMediator mediator)
	: IBackgroundJobHandler
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	public string Kind => "seed-data";
	public bool RequiresWriteAccess => true;

	public async Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken)
	{
		var request = Parse(parameters);
		if (request is null) return Result.Failure(new Error("Job.InvalidParameters", "A valid seed request is required."));
		var validation = await new SeedJobParametersValidator().ValidateAsync(request, cancellationToken);
		if (!validation.IsValid) return Result.Failure(new Error("Job.InvalidParameters", string.Join(" ", validation.Errors)));
		var connection = await repository.GetConnectionByIdAsync(connectionId, cancellationToken);
		if (connection.IsFailure) return Result.Failure(connection.Error);
		if (!await access.CanAccessAsync(connection.Value)) return Result.Failure(new Error("Connection.Forbidden", "Access to the connection is denied."));
		if (connection.Value.IsReadOnly) return Result.Failure(DomainErrors.Connection.ReadOnlyMode);
		return Result.Success();
	}

	public async Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters, Func<double?, object?, Task> reportProgress,
		CancellationToken cancellationToken)
	{
		var request = Parse(parameters);
		if (request is null) return Result.Failure<object?>(new Error("Job.InvalidParameters", "A valid seed request is required."));
		var clock = Stopwatch.StartNew();
		var command = new SeedSampleDataCommand(connectionId, request, async (count, total) =>
		{
			if (count != 1 && clock.ElapsedMilliseconds < 250) return;
			clock.Restart();
			await reportProgress((double)count / total * 100, new { created = count, total });
		}, cancellationToken);
		// Allow a cancelled operation to return and audit its confirmed partial result.
		var result = await mediator.Send(command, CancellationToken.None);
		if (result.IsFailure) return Result.Failure<object?>(result.Error);
		return Result.Success<object?>(result.Value.Error is null ? result.Value : new BackgroundJobOutcome(result.Value, result.Value.Error));
	}

	private static SeedSampleDataOptions? Parse(JsonElement parameters)
	{
		if (parameters.ValueKind != JsonValueKind.Object) return null;
		try { return parameters.Deserialize<SeedSampleDataOptions>(JsonOptions); }
		catch (JsonException) { return null; }
	}
}

internal sealed class SeedJobParametersValidator : AbstractValidator<SeedSampleDataOptions>
{
	public SeedJobParametersValidator()
	{
		RuleFor(x => x.TotalKeys).InclusiveBetween(1, 10000);
		RuleFor(x => x.RandomDatabasesCount).InclusiveBetween(1, 16).When(x => x.RandomizeDatabases);
		RuleFor(x => x.Databases).Must(dbs => dbs is null || (dbs.Count is > 0 and <= 16 && dbs.All(db => db is >= 0 and <= 15)))
			.WithMessage("Databases must contain valid database numbers.");
		RuleFor(x => x.DataTypes).Must(types => types is null || (types.Count is > 0 and <= 6 && types.All(type => type is "string" or "hash" or "list" or "set" or "zset" or "stream")))
			.WithMessage("Data types must be supported Redis types.");
		RuleFor(x => x.KeyComplexity).Must(value => value is "simple" or "hierarchical" or "uuid" or "hashtag");
		RuleFor(x => x.PayloadComplexity).Must(value => value is "simple" or "json" or "complex");
		RuleFor(x => x.TtlStrategy).Must(value => value is "mixed" or "persistent" or "expiring");
		RuleFor(x => x.CustomPrefix).MaximumLength(128);
	}
}
