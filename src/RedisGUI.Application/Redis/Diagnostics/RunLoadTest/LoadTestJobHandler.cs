using System;
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

namespace RedisGUI.Application.Redis.Commands.RunLoadTest;

/// <summary>Validates and executes load tests in an independent background scope.</summary>
public sealed class LoadTestJobHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IMediator mediator)
	: IBackgroundJobHandler
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	public string Kind => "load-test";
	public bool RequiresWriteAccess => true;

	public async Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken)
	{
		var request = Parse(parameters);
		if (request is null) return Result.Failure(new Error("Job.InvalidParameters", "A valid load test request is required."));
		var validation = await new LoadTestJobParametersValidator().ValidateAsync(request, cancellationToken);
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
		if (request is null) return Result.Failure<object?>(new Error("Job.InvalidParameters", "A valid load test request is required."));
		var command = new RunStreamingLoadTestCommand(connectionId, request, progress =>
		{
			double? percent = progress.TotalDurationSeconds > 0
				? progress.ElapsedSeconds / progress.TotalDurationSeconds * 100
				: progress.TotalOperations > 0 ? (double)progress.CompletedOperations / progress.TotalOperations * 100 : null;
			return reportProgress(percent, progress);
		});
		var result = await mediator.Send<RedisLoadTestResult>(command, cancellationToken);
		return result.IsSuccess ? Result.Success<object?>(result.Value) : Result.Failure<object?>(result.Error);
	}

	private static RedisLoadTestRequest? Parse(JsonElement parameters)
	{
		if (parameters.ValueKind != JsonValueKind.Object) return null;
		try { return parameters.Deserialize<RedisLoadTestRequest>(JsonOptions); }
		catch (JsonException) { return null; }
	}
}

internal sealed class LoadTestJobParametersValidator : AbstractValidator<RedisLoadTestRequest>
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
