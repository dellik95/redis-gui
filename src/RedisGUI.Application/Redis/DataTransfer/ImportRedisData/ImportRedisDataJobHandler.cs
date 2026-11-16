using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

public sealed record ImportJobParameters(string Data, string Format = "json", bool Overwrite = false, int Database = 0);

/// <summary>Runs a bounded import in a background job and retains partial cancellation results.</summary>
public sealed class ImportRedisDataJobHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IMediator mediator)
	: IBackgroundJobHandler
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	public string Kind => "redis-import";
	public bool RequiresWriteAccess => true;
	public int MaxParameterCharacters => 24 * 1024 * 1024;

	public async Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken)
	{
		var request = Parse(parameters);
		if (request is null || request.Format is not ("json" or "csv"))
			return Result.Failure(new Error("Job.InvalidParameters", "A valid import request is required."));
		var command = ToCommand(connectionId, request);
		var validation = await new ImportRedisDataCommandValidator().ValidateAsync(command, cancellationToken);
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
		if (request is null) return Result.Failure<object?>(new Error("Job.InvalidParameters", "A valid import request is required."));
		var clock = Stopwatch.StartNew();
		var command = ToCommand(connectionId, request) with
		{
			OperationCancellation = cancellationToken,
			OnProgress = async progress =>
			{
				if (progress.Processed != 1 && clock.ElapsedMilliseconds < 250) return;
				clock.Restart();
				await reportProgress(progress.Total == 0 ? null : (double)progress.Processed / progress.Total * 100, progress);
			}
		};
		// Preserve the partial result and audit record after cooperative cancellation.
		var result = await mediator.Send(command, CancellationToken.None);
		if (result.IsFailure) return Result.Failure<object?>(result.Error);
		var json = JsonSerializer.SerializeToUtf8Bytes(result.Value, JsonOptions);
		if (json.Length <= 16384) return Result.Success<object?>(result.Value);
		var path = Path.Combine(Path.GetTempPath(), "redisgui-job-import-result-" + Guid.NewGuid().ToString("N") + ".json");
		try
		{
			await File.WriteAllBytesAsync(path, json, CancellationToken.None);
			return Result.Success<object?>(new BackgroundJobFile(path, "import-result.json", "application/json"));
		}
		catch { File.Delete(path); throw; }
	}

	private static ImportRedisDataCommand ToCommand(Guid connectionId, ImportJobParameters request) =>
		new(connectionId, request.Data, request.Format == "csv" ? ImportFormat.Csv : ImportFormat.Json, request.Overwrite, request.Database);

	private static ImportJobParameters? Parse(JsonElement parameters)
	{
		if (parameters.ValueKind != JsonValueKind.Object) return null;
		try { return parameters.Deserialize<ImportJobParameters>(JsonOptions); }
		catch (JsonException) { return null; }
	}
}
