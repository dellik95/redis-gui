using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.RunLoadTest;

/// <summary>Validates and executes load tests in an independent background scope.</summary>
public sealed class LoadTestJobHandler(
	IRedisConnectionRepository repository,
	IConnectionAccessService access,
	IMediator mediator,
	IValidator<RedisLoadTestRequest>? validator = null)
	: BackgroundJobHandler<RedisLoadTestRequest, RedisLoadTestProgress, RedisLoadTestResult>(repository, access, validator)
{
	public override string Kind => "load-test";
	public override string DisplayName => "Redis Load Test";
	public override bool RequiresWriteAccess => true;

	protected override IValidator<RedisLoadTestRequest> CreateFallbackValidator() => new LoadTestJobParametersValidator();

	protected override async Task<Result<JobExecutionOutcome<RedisLoadTestResult>>> ExecuteTypedAsync(
		Guid connectionId,
		RedisLoadTestRequest request,
		IJobProgressReporter<RedisLoadTestProgress> progress,
		CancellationToken cancellationToken)
	{
		var command = new RunStreamingLoadTestCommand(connectionId, request, itemProgress =>
		{
			double? percent = itemProgress.TotalDurationSeconds > 0
				? itemProgress.ElapsedSeconds / itemProgress.TotalDurationSeconds * 100
				: itemProgress.TotalOperations > 0 ? (double)itemProgress.CompletedOperations / itemProgress.TotalOperations * 100 : null;
			return progress.ReportAsync(percent, itemProgress);
		});

		var result = await mediator.Send<RedisLoadTestResult>(command, cancellationToken);
		return result.IsSuccess
			? Result.Success(JobExecutionOutcome<RedisLoadTestResult>.Completed(result.Value))
			: Result.Failure<JobExecutionOutcome<RedisLoadTestResult>>(result.Error);
	}
}

public sealed class LoadTestJobParametersValidator : AbstractValidator<RedisLoadTestRequest>
{
	public LoadTestJobParametersValidator()
	{
		RuleFor(x => x.DurationSeconds).InclusiveBetween(0, 600);
		RuleFor(x => x.OperationsCount).InclusiveBetween(10, 100000).When(x => x.DurationSeconds == 0);
		RuleFor(x => x.Concurrency).InclusiveBetween(1, 50);
		RuleFor(x => x.PayloadSizeBytes).InclusiveBetween(16, 65536);
		RuleFor(x => x.Database).InclusiveBetween(0, 15).When(x => x.Database.HasValue);
		RuleFor(x => x.OperationType).Must(x => x is "read" or "write" or "mixed");
		RuleFor(x => x.KeyPrefix).MaximumLength(256);
	}
}
