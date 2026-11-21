using DispatchWeave;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

public sealed record ImportJobParameters(string Data, string Format = "json", bool Overwrite = false, int Database = 0);

/// <summary>Runs a bounded import in a background job and retains partial cancellation results.</summary>
public sealed class ImportRedisDataJobHandler(
	IRedisConnectionRepository repository,
	IConnectionAccessService access,
	IDispatcher mediator,
	IValidator<ImportJobParameters>? validator = null)
	: BackgroundJobHandler<ImportJobParameters, ImportRedisDataProgress, ImportRedisDataResponse>(repository, access, validator)
{
	public override string Kind => "redis-import";
	public override string DisplayName => "Redis Import";
	public override bool RequiresWriteAccess => true;
	public override int MaxParameterCharacters => 24 * 1024 * 1024;

	protected override IValidator<ImportJobParameters> CreateFallbackValidator() => new ImportJobParametersValidator();

	protected override async Task<Result<JobExecutionOutcome<ImportRedisDataResponse>>> ExecuteTypedAsync(
		Guid connectionId,
		ImportJobParameters request,
		IJobProgressReporter<ImportRedisDataProgress> progress,
		CancellationToken cancellationToken)
	{
		var clock = Stopwatch.StartNew();
		var command = new ImportRedisDataCommand(
			connectionId,
			request.Data,
			request.Format == "csv" ? ImportFormat.Csv : ImportFormat.Json,
			request.Overwrite,
			request.Database,
			OnProgress: async itemProgress =>
			{
				if (itemProgress.Processed != 1 && clock.ElapsedMilliseconds < 250) return;
				clock.Restart();
				await progress.ReportAsync(
					itemProgress.Total == 0 ? null : (double)itemProgress.Processed / itemProgress.Total * 100,
					itemProgress);
			},
			OperationCancellation: cancellationToken);

		// Preserve the partial result and audit record after cooperative cancellation.
		var result = await mediator.Send(command, CancellationToken.None);
		if (result.IsFailure)
		{
			return Result.Failure<JobExecutionOutcome<ImportRedisDataResponse>>(result.Error);
		}

		int processedTotal = result.Value.ImportedCount + result.Value.SkippedCount + result.Value.FailedCount;
		await progress.ReportAsync(
			100,
			new ImportRedisDataProgress(
				processedTotal,
				processedTotal,
				result.Value.ImportedCount,
				result.Value.SkippedCount,
				result.Value.FailedCount));

		var json = JsonSerializer.SerializeToUtf8Bytes(result.Value, JsonOptions);
		if (json.Length <= 16384)
		{
			return Result.Success(JobExecutionOutcome<ImportRedisDataResponse>.Completed(result.Value));
		}

		var path = BackgroundJobTempStorage.CreateFilePath("redisgui-job-import-result", ".json");
		try
		{
			await File.WriteAllBytesAsync(path, json, CancellationToken.None);
			return Result.Success(JobExecutionOutcome<ImportRedisDataResponse>.FileOutcome(
				new BackgroundJobFile(path, "import-result.json", "application/json")));
		}
		catch
		{
			if (File.Exists(path)) File.Delete(path);
			throw;
		}
	}
}

public sealed class ImportJobParametersValidator : AbstractValidator<ImportJobParameters>
{
	public ImportJobParametersValidator()
	{
		RuleFor(x => x.Data).NotEmpty().MaximumLength(20 * 1024 * 1024);
		RuleFor(x => x.Format).Must(value => value is "json" or "csv")
			.WithMessage("Format must be json or csv.");
		RuleFor(x => x.Database).InclusiveBetween(0, 15);
	}
}
