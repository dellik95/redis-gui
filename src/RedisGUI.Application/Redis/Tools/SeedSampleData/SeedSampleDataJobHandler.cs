using DispatchWeave;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.SeedSampleData;

public sealed record SeedJobProgress(
	int Created,
	int Total,
	int Requested = 0,
	int Processed = 0,
	int Persisted = 0,
	int Failed = 0);

/// <summary>Runs bounded sample-data seeding as a cancellable background job.</summary>
public sealed class SeedSampleDataJobHandler(
	IRedisConnectionRepository repository,
	IConnectionAccessService access,
	IDispatcher mediator,
	IValidator<SeedSampleDataOptions>? validator = null)
	: BackgroundJobHandler<SeedSampleDataOptions, SeedJobProgress, SeedDataResult>(repository, access, validator)
{
	public override string Kind => "seed-data";
	public override string DisplayName => "Seed Sample Data";
	public override bool RequiresWriteAccess => true;

	protected override IValidator<SeedSampleDataOptions> CreateFallbackValidator() => new SeedJobParametersValidator();

	protected override async Task<Result<JobExecutionOutcome<SeedDataResult>>> ExecuteTypedAsync(
		Guid connectionId,
		SeedSampleDataOptions request,
		IJobProgressReporter<SeedJobProgress> progress,
		CancellationToken cancellationToken)
	{
		var clock = Stopwatch.StartNew();
		var command = new SeedSampleDataCommand(connectionId, request, async (count, total) =>
		{
			if (count != 1 && count < total && clock.ElapsedMilliseconds < 250) return;
			clock.Restart();
			await progress.ReportAsync(
				total > 0 ? (double)count / total * 100 : 100,
				new SeedJobProgress(count, total, total, count, count, 0));
		}, cancellationToken);

		// Allow a cancelled operation to return and audit its confirmed partial result.
		var result = await mediator.Send(command, CancellationToken.None);
		if (result.IsFailure)
		{
			return Result.Failure<JobExecutionOutcome<SeedDataResult>>(result.Error);
		}

		var outcome = result.Value;
		var requested = outcome.RequestedKeys > 0 ? outcome.RequestedKeys : request.TotalKeys;
		var processed = outcome.ProcessedKeys > 0 ? outcome.ProcessedKeys : outcome.TotalKeysSeeded + outcome.FailedKeys;
		var persisted = outcome.TotalKeysSeeded;
		var failed = outcome.FailedKeys;

		await progress.ReportAsync(
			requested > 0 ? (double)persisted / requested * 100 : 100,
			new SeedJobProgress(persisted, requested, requested, processed, persisted, failed));

		return Result.Success(outcome.Error is null
			? JobExecutionOutcome<SeedDataResult>.Completed(outcome)
			: JobExecutionOutcome<SeedDataResult>.Failed(outcome.Error, outcome, cancellationToken.IsCancellationRequested));
	}
}

public sealed class SeedJobParametersValidator : AbstractValidator<SeedSampleDataOptions>
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
