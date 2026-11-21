using FluentResults;
using DispatchWeave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.BulkUpdateTtl;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.BulkOperations;

public sealed record BulkKeyJobParameters(
	string Operation,
	int Database,
	string? Pattern = null,
	string[]? Keys = null,
	long? TtlSeconds = null,
	string?[]? KeysBase64 = null);
public sealed record BulkKeyJobProgress(int Processed, int? Total, int Changed, int Unchanged, int Failed);
public sealed record BulkKeyJobResult(int Processed, int? Total, int Changed, int Unchanged, int Failed,
	IReadOnlyList<string> FailedKeys, bool Cancelled, bool MayHaveUnconfirmedChanges, string? Error);

/// <summary>Runs confirmed bulk key changes in bounded, cancellable background batches.</summary>
public sealed class BulkKeyJobHandler(
	IRedisConnectionRepository repository,
	IConnectionAccessService access,
	IDispatcher mediator,
	IValidator<BulkKeyJobParameters>? validator = null)
	: BackgroundJobHandler<BulkKeyJobParameters, BulkKeyJobProgress, BulkKeyJobResult>(repository, access, validator)
{
	public override string Kind => "bulk-keys";
	public override string DisplayName => "Bulk Key Operation";
	public override bool RequiresWriteAccess => true;
	public override int MaxParameterCharacters => 6 * 1024 * 1024;

	protected override IValidator<BulkKeyJobParameters> CreateFallbackValidator() => new BulkKeyJobParametersValidator();

	protected override async Task<Result<JobExecutionOutcome<BulkKeyJobResult>>> ExecuteTypedAsync(
		Guid connectionId,
		BulkKeyJobParameters request,
		IJobProgressReporter<BulkKeyJobProgress> progress,
		CancellationToken cancellationToken)
	{
		if (request.Operation == "delete-pattern")
		{
			var clock = Stopwatch.StartNew();
			var command = new StreamDeleteKeysByPatternCommand(connectionId, request.Pattern!, request.Database, async count =>
			{
				if (count != 1 && clock.ElapsedMilliseconds < 250) return;
				clock.Restart();
				await progress.ReportAsync(null, new BulkKeyJobProgress(count, null, count, 0, 0));
			});
			var patternResult = await mediator.Send(command, cancellationToken);
			if (patternResult.IsFailed)
			{
				return Result.Fail<JobExecutionOutcome<BulkKeyJobResult>>(patternResult.Errors);
			}

			var result = patternResult.Value;
			await progress.ReportAsync(null, new BulkKeyJobProgress(result.DeletedCount, null, result.DeletedCount, 0, 0));
			var summary = new BulkKeyJobResult(result.DeletedCount, null, result.DeletedCount, 0, 0, [],
				result.Cancelled, result.Error is not null, result.Error);
			return Result.Ok(result.Error is null
				? JobExecutionOutcome<BulkKeyJobResult>.Completed(summary)
				: JobExecutionOutcome<BulkKeyJobResult>.Failed(result.Error, summary, cancellationToken.IsCancellationRequested));
		}

		string[] keys = request.Keys!;
		string?[]? keysBase64 = request.KeysBase64;
		int processed = 0, changed = 0, unchanged = 0, failed = 0;
		List<string> failedExamples = [];
		for (int offset = 0; offset < keys.Length; offset += 100)
		{
			if (cancellationToken.IsCancellationRequested) break;
			int take = Math.Min(100, keys.Length - offset);
			string[] batch = keys[offset..(offset + take)];
			string?[]? batchBase64 = keysBase64 is null || offset >= keysBase64.Length
				? null
				: keysBase64[offset..Math.Min(offset + take, keysBase64.Length)];

			if (request.Operation == "delete")
			{
				var result = await mediator.Send(
					new DeleteMultipleKeysCommand(connectionId, batch, request.Database, batchBase64),
					CancellationToken.None);
				if (result.IsFailed)
				{
					var summary = new BulkKeyJobResult(processed, keys.Length, changed, unchanged, failed,
						failedExamples, cancellationToken.IsCancellationRequested, true, result.GetDomainError().Message);
					return Result.Ok(JobExecutionOutcome<BulkKeyJobResult>.Failed(result.GetDomainError().Message, summary, cancellationToken.IsCancellationRequested));
				}
				changed += result.Value.DeletedCount;
				failed += result.Value.FailedKeys.Count;
				failedExamples.AddRange(result.Value.FailedKeys.Take(Math.Max(0, 5 - failedExamples.Count)));
			}
			else
			{
				var result = await mediator.Send(
					new BulkUpdateTtlCommand(connectionId, batch, request.TtlSeconds, request.Database, batchBase64),
					CancellationToken.None);
				if (result.IsFailed)
				{
					var summary = new BulkKeyJobResult(processed, keys.Length, changed, unchanged, failed,
						failedExamples, cancellationToken.IsCancellationRequested, true, result.GetDomainError().Message);
					return Result.Ok(JobExecutionOutcome<BulkKeyJobResult>.Failed(result.GetDomainError().Message, summary, cancellationToken.IsCancellationRequested));
				}
				changed += result.Value.UpdatedCount;
				unchanged += result.Value.UnchangedCount;
				failed += result.Value.FailedKeys.Count;
				failedExamples.AddRange(result.Value.FailedKeys.Take(Math.Max(0, 5 - failedExamples.Count)));
			}
			processed += batch.Length;
			await progress.ReportAsync((double)processed / keys.Length * 100, new BulkKeyJobProgress(processed, keys.Length, changed, unchanged, failed));
		}

		return Result.Ok(JobExecutionOutcome<BulkKeyJobResult>.Completed(
			new BulkKeyJobResult(processed, keys.Length, changed, unchanged, failed, failedExamples,
				cancellationToken.IsCancellationRequested && processed < keys.Length, false, null)));
	}
}

public sealed class BulkKeyJobParametersValidator : AbstractValidator<BulkKeyJobParameters>
{
	public BulkKeyJobParametersValidator()
	{
		RuleFor(x => x.Operation).Must(value => value is "delete-pattern" or "delete" or "ttl")
			.WithMessage("Operation must be delete-pattern, delete, or ttl.");
		RuleFor(x => x.Database).InclusiveBetween(0, 15);
		When(x => x.Operation == "delete-pattern", () => RuleFor(x => x.Pattern).NotEmpty().MaximumLength(256));
		When(x => x.Operation is "delete" or "ttl", () =>
		{
			RuleFor(x => x.Keys).NotNull()
				.Must((cmd, keys) => keys is { Length: > 0 and <= 10000 }
					&& HasValidAndDistinctIdentities(keys, cmd.KeysBase64))
				.WithMessage("Provide 1 to 10,000 distinct valid keys.");
			RuleFor(x => x.KeysBase64)
				.Must((cmd, b64) => b64 is null || (cmd.Keys is not null && b64.Length == cmd.Keys.Length))
				.WithMessage("KeysBase64 length must match Keys length.");
		});
		RuleFor(x => x.TtlSeconds).InclusiveBetween(1, 922337203685).When(x => x.Operation == "ttl" && x.TtlSeconds.HasValue);
	}

	private static bool HasValidAndDistinctIdentities(string[] keys, string?[]? keysBase64)
	{
		if (keysBase64 is not null && keysBase64.Length != keys.Length)
			return false;

		var seen = new HashSet<string>(keys.Length, StringComparer.Ordinal);
		for (int i = 0; i < keys.Length; i++)
		{
			string? rawB64 = keysBase64?[i];
			if (!string.IsNullOrEmpty(rawB64))
			{
				if (rawB64.Length > 1024 || !IsValidNonEmptyBase64(rawB64))
					return false;

				if (!seen.Add("b64:" + rawB64))
					return false;
			}
			else
			{
				string key = keys[i];
				if (string.IsNullOrEmpty(key) || key.Length > 512)
					return false;

				if (!seen.Add("txt:" + key))
					return false;
			}
		}

		return true;
	}

	private static bool IsValidNonEmptyBase64(string base64)
	{
		Span<byte> scratch = base64.Length <= 1024 ? stackalloc byte[base64.Length] : new byte[base64.Length];
		return Convert.TryFromBase64String(base64, scratch, out int written) && written > 0;
	}
}
