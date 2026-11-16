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
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

public sealed record ExportJobParameters(string Pattern = "*", string Format = "json", int Database = 0);

/// <summary>Runs a Redis export in a background job and stores the result on disk.</summary>
public sealed class ExportRedisDataJobHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IMediator mediator)
	: IBackgroundJobHandler
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	public string Kind => "redis-export";
	public bool RequiresWriteAccess => true;

	public async Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken)
	{
		var request = Parse(parameters);
		if (request is null) return Result.Failure(new Error("Job.InvalidParameters", "A valid export request is required."));
		var validation = await new ExportJobParametersValidator().ValidateAsync(request, cancellationToken);
		if (!validation.IsValid) return Result.Failure(new Error("Job.InvalidParameters", string.Join(" ", validation.Errors)));
		var connection = await repository.GetConnectionByIdAsync(connectionId, cancellationToken);
		if (connection.IsFailure) return Result.Failure(connection.Error);
		return await access.CanAccessAsync(connection.Value)
			? Result.Success() : Result.Failure(new Error("Connection.Forbidden", "Access to the connection is denied."));
	}

	public async Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters, Func<double?, object?, Task> reportProgress,
		CancellationToken cancellationToken)
	{
		var request = Parse(parameters);
		if (request is null) return Result.Failure<object?>(new Error("Job.InvalidParameters", "A valid export request is required."));
		var clock = Stopwatch.StartNew();
		int exported = 0;
		var query = new ExportRedisDataQuery(connectionId, request.Pattern,
			request.Format == "csv" ? ExportFormat.Csv : ExportFormat.Json, request.Database, async count =>
			{
				exported = count;
				if (count != 1 && clock.ElapsedMilliseconds < 250) return;
				clock.Restart();
				await reportProgress(null, new { exportedCount = count });
			});
		var result = await mediator.Send(query, cancellationToken);
		if (result.IsFailure) return Result.Failure<object?>(result.Error);
		await reportProgress(null, new { exportedCount = exported });
		await using var source = result.Value.Data;
		var path = Path.Combine(Path.GetTempPath(), "redisgui-job-export-" + Guid.NewGuid().ToString("N") + ".tmp");
		try
		{
			await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 65536, FileOptions.Asynchronous))
				await source.CopyToAsync(output, cancellationToken);
			return Result.Success<object?>(new BackgroundJobFile(path, result.Value.FileName, result.Value.ContentType));
		}
		catch { File.Delete(path); throw; }
	}

	private static ExportJobParameters? Parse(JsonElement parameters)
	{
		if (parameters.ValueKind != JsonValueKind.Object) return null;
		try { return parameters.Deserialize<ExportJobParameters>(JsonOptions); }
		catch (JsonException) { return null; }
	}
}

internal sealed class ExportJobParametersValidator : AbstractValidator<ExportJobParameters>
{
	public ExportJobParametersValidator()
	{
		RuleFor(x => x.Pattern).NotEmpty().MaximumLength(1024);
		RuleFor(x => x.Format).Must(value => value is "json" or "csv");
		RuleFor(x => x.Database).InclusiveBetween(0, 15);
	}
}
