using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.BulkUpdateTtl;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.BulkOperations;

public sealed record BulkKeyJobParameters(string Operation, int Database, string? Pattern = null, string[]? Keys = null, long? TtlSeconds = null);
public sealed record BulkKeyJobResult(int Processed, int? Total, int Changed, int Unchanged, int Failed,
	IReadOnlyList<string> FailedKeys, bool Cancelled, bool MayHaveUnconfirmedChanges, string? Error);

/// <summary>Runs confirmed bulk key changes in bounded, cancellable background batches.</summary>
public sealed class BulkKeyJobHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IMediator mediator)
	: IBackgroundJobHandler
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	public string Kind => "bulk-keys";
	public bool RequiresWriteAccess => true;
	public int MaxParameterCharacters => 6 * 1024 * 1024;

	public async Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken)
	{
		var request = Parse(parameters);
		if (request is null) return Result.Failure(new Error("Job.InvalidParameters", "A valid bulk operation is required."));
		var validation = await new BulkKeyJobParametersValidator().ValidateAsync(request, cancellationToken);
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
		if (request is null) return Result.Failure<object?>(new Error("Job.InvalidParameters", "A valid bulk operation is required."));
		if (request.Operation == "delete-pattern")
		{
			var clock = Stopwatch.StartNew();
			var command = new StreamDeleteKeysByPatternCommand(connectionId, request.Pattern!, request.Database, async count =>
			{
				if (count != 1 && clock.ElapsedMilliseconds < 250) return;
				clock.Restart();
				await reportProgress(null, new { processed = count, changed = count });
			});
			var patternResult = await mediator.Send(command, cancellationToken);
			if (patternResult.IsFailure) return Result.Failure<object?>(patternResult.Error);
			var result = patternResult.Value;
			await reportProgress(null, new { processed = result.DeletedCount, changed = result.DeletedCount });
			var summary = new BulkKeyJobResult(result.DeletedCount, null, result.DeletedCount, 0, 0, [],
				result.Cancelled, result.Error is not null, result.Error);
			return Result.Success<object?>(result.Error is null ? summary : new BackgroundJobOutcome(summary, result.Error));
		}

		string[] keys = request.Keys!;
		int processed = 0, changed = 0, unchanged = 0, failed = 0;
		List<string> failedExamples = [];
		foreach (var batch in keys.Chunk(100))
		{
			if (cancellationToken.IsCancellationRequested) break;
			if (request.Operation == "delete")
			{
				var result = await mediator.Send(new DeleteMultipleKeysCommand(connectionId, batch, request.Database), CancellationToken.None);
				if (result.IsFailure) return Result.Success<object?>(new BackgroundJobOutcome(new BulkKeyJobResult(processed, keys.Length, changed, unchanged, failed,
					failedExamples, false, true, result.Error.Message), result.Error.Message));
				changed += result.Value.DeletedCount;
				failed += result.Value.FailedKeys.Count;
				failedExamples.AddRange(result.Value.FailedKeys.Take(Math.Max(0, 5 - failedExamples.Count)));
			}
			else
			{
				var result = await mediator.Send(new BulkUpdateTtlCommand(connectionId, batch, request.TtlSeconds, request.Database), CancellationToken.None);
				if (result.IsFailure) return Result.Success<object?>(new BackgroundJobOutcome(new BulkKeyJobResult(processed, keys.Length, changed, unchanged, failed,
					failedExamples, false, true, result.Error.Message), result.Error.Message));
				changed += result.Value.UpdatedCount;
				unchanged += result.Value.UnchangedCount;
				failed += result.Value.FailedKeys.Count;
				failedExamples.AddRange(result.Value.FailedKeys.Take(Math.Max(0, 5 - failedExamples.Count)));
			}
			processed += batch.Length;
			await reportProgress((double)processed / keys.Length * 100, new { processed, total = keys.Length, changed, unchanged, failed });
		}
		return Result.Success<object?>(new BulkKeyJobResult(processed, keys.Length, changed, unchanged, failed, failedExamples,
			cancellationToken.IsCancellationRequested && processed < keys.Length, false, null));
	}

	private static BulkKeyJobParameters? Parse(JsonElement parameters)
	{
		if (parameters.ValueKind != JsonValueKind.Object) return null;
		try { return parameters.Deserialize<BulkKeyJobParameters>(JsonOptions); }
		catch (JsonException) { return null; }
	}
}

internal sealed class BulkKeyJobParametersValidator : AbstractValidator<BulkKeyJobParameters>
{
	public BulkKeyJobParametersValidator()
	{
		RuleFor(x => x.Operation).Must(value => value is "delete-pattern" or "delete" or "ttl");
		RuleFor(x => x.Database).InclusiveBetween(0, 15);
		When(x => x.Operation == "delete-pattern", () => RuleFor(x => x.Pattern).NotEmpty().MaximumLength(256));
		When(x => x.Operation is "delete" or "ttl", () =>
		{
			RuleFor(x => x.Keys).NotNull().Must(keys => keys is { Length: > 0 and <= 10000 } && keys.Distinct(StringComparer.Ordinal).Count() == keys.Length)
				.WithMessage("Provide 1 to 10,000 distinct keys.");
			RuleForEach(x => x.Keys).NotEmpty().MaximumLength(512);
		});
		RuleFor(x => x.TtlSeconds).InclusiveBetween(1, 922337203685).When(x => x.Operation == "ttl" && x.TtlSeconds.HasValue);
	}
}
