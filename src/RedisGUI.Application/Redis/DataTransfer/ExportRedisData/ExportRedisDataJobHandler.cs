using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

public sealed record ExportJobParameters(
	string Pattern = "*",
	string Format = "json",
	int Database = 0,
	string[]? Keys = null,
	string?[]? KeysBase64 = null);
public sealed record ExportJobProgress(int ExportedCount, int? Total = null);

/// <summary>Runs a Redis export in a background job and stores the result on disk.</summary>
public sealed class ExportRedisDataJobHandler(
	IRedisConnectionRepository repository,
	IConnectionAccessService access,
	IMediator mediator,
	IValidator<ExportJobParameters>? validator = null)
	: BackgroundJobHandler<ExportJobParameters, ExportJobProgress, BackgroundJobFile>(repository, access, validator)
{
	public override string Kind => "redis-export";
	public override string DisplayName => "Redis Export";
	public override bool RequiresWriteAccess => true;
	protected override bool RequiresNonReadOnlyConnection => false;

	protected override IValidator<ExportJobParameters> CreateFallbackValidator() => new ExportJobParametersValidator();

	protected override async Task<Result<JobExecutionOutcome<BackgroundJobFile>>> ExecuteTypedAsync(
		Guid connectionId,
		ExportJobParameters request,
		IJobProgressReporter<ExportJobProgress> progress,
		CancellationToken cancellationToken)
	{
		var clock = Stopwatch.StartNew();
		int exported = 0;
		int? totalKeys = request.Keys is { Length: > 0 } ? request.Keys.Length : null;
		var query = new ExportRedisDataQuery(
			connectionId,
			string.IsNullOrWhiteSpace(request.Pattern) ? "*" : request.Pattern,
			request.Format == "csv" ? ExportFormat.Csv : ExportFormat.Json,
			request.Database,
			async count =>
			{
				exported = count;
				if (count != 1 && (totalKeys is null || count < totalKeys.Value) && clock.ElapsedMilliseconds < 250) return;
				clock.Restart();
				double? pct = totalKeys is > 0 ? (double)count / totalKeys.Value * 100 : null;
				await progress.ReportAsync(pct, new ExportJobProgress(count, totalKeys));
			},
			request.Keys,
			request.KeysBase64);
		var result = await mediator.Send(query, cancellationToken);
		if (result.IsFailure)
		{
			return Result.Failure<JobExecutionOutcome<BackgroundJobFile>>(result.Error);
		}

		await progress.ReportAsync(totalKeys is > 0 ? 100 : null, new ExportJobProgress(exported, totalKeys));
		await using var source = result.Value.Data;
		var path = BackgroundJobTempStorage.CreateFilePath("redisgui-job-export", ".tmp");
		try
		{
			await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 65536, FileOptions.Asynchronous))
			{
				await source.CopyToAsync(output, cancellationToken);
			}
			return Result.Success(JobExecutionOutcome<BackgroundJobFile>.FileOutcome(
				new BackgroundJobFile(path, result.Value.FileName, result.Value.ContentType)));
		}
		catch
		{
			if (File.Exists(path)) File.Delete(path);
			throw;
		}
	}
}

public sealed class ExportJobParametersValidator : AbstractValidator<ExportJobParameters>
{
	public ExportJobParametersValidator()
	{
		RuleFor(x => x.Pattern)
			.NotEmpty()
			.MaximumLength(1024)
			.When(x => x.Keys is null || x.Keys.Length == 0);
		RuleFor(x => x.Keys)
			.Must(keys => keys is null || (keys.Length is > 0 and <= 10000))
			.WithMessage("Selected keys must contain between 1 and 10,000 items.");
		RuleFor(x => x.Format).Must(value => value is "json" or "csv")
			.WithMessage("Format must be json or csv.");
		RuleFor(x => x.Database).InclusiveBetween(0, 15);
	}
}
