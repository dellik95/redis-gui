using FluentValidation;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Services;

/// <summary>Application-owned implementation of a supported background operation.</summary>
public interface IBackgroundJobHandler
{
	string Kind { get; }
	string DisplayName => Kind;
	bool RequiresWriteAccess { get; }
	/// <summary>Maximum serialized parameter length accepted from the client.</summary>
	int MaxParameterCharacters => 16384;
	Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken);
	Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters,
		Func<double?, object?, Task> reportProgress, CancellationToken cancellationToken);
}

/// <summary>Typed progress callback supplied to background job handlers.</summary>
public interface IJobProgressReporter<in TProgress>
{
	Task ReportAsync(double? progressPercent, TProgress metrics);
}

/// <summary>Strongly typed terminal outcome returned by a background job handler.</summary>
public abstract record JobExecutionOutcome<TResult>
{
	private JobExecutionOutcome() { }

	public sealed record Success(TResult Value) : JobExecutionOutcome<TResult>;
	public sealed record PartialFailure(string Error, TResult? PartialResult = default, bool CancellationRequested = false) : JobExecutionOutcome<TResult>;
	public sealed record Cancelled(TResult? PartialResult = default, string? Message = null) : JobExecutionOutcome<TResult>;
	public sealed record File(BackgroundJobFile JobFile) : JobExecutionOutcome<TResult>;

	public static JobExecutionOutcome<TResult> Completed(TResult value) => new Success(value);
	public static JobExecutionOutcome<TResult> Failed(string error, TResult? partialResult = default, bool cancellationRequested = false) =>
		new PartialFailure(error, partialResult, cancellationRequested);
	public static JobExecutionOutcome<TResult> CancelledOutcome(TResult? partialResult = default, string? message = null) =>
		new Cancelled(partialResult, message);
	public static JobExecutionOutcome<TResult> FileOutcome(BackgroundJobFile file) => new File(file);
}

/// <summary>Typed, extensible base class for background job handlers with shared FluentValidation and authorization.</summary>
public abstract class BackgroundJobHandler<TParameters, TProgress, TResult>(
	IRedisConnectionRepository repository,
	IConnectionAccessService access,
	IValidator<TParameters>? validator = null) : IBackgroundJobHandler
	where TParameters : class
{
	protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public abstract string Kind { get; }
	public virtual string DisplayName => Kind;
	public abstract bool RequiresWriteAccess { get; }
	protected virtual bool RequiresNonReadOnlyConnection => RequiresWriteAccess;
	public virtual int MaxParameterCharacters => 16384;

	public async Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken)
	{
		var request = ParseParameters(parameters);
		if (request is null)
		{
			return Result.Failure(new ValidationFailureError(new Dictionary<string, string[]>
			{
				["parameters"] = [$"Valid parameters are required for '{Kind}'."]
			}));
		}

		var effectiveValidator = validator ?? CreateFallbackValidator();
		if (effectiveValidator is not null)
		{
			var validation = await effectiveValidator.ValidateAsync(request, cancellationToken);
			if (!validation.IsValid)
			{
				var errors = validation.Errors
					.GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "parameters" : error.PropertyName)
					.ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
				return Result.Failure(new ValidationFailureError(errors));
			}
		}

		var connectionResult = await repository.GetAuthorizedConnectionAsync(
			connectionId,
			access,
			cancellationToken,
			RequiresNonReadOnlyConnection);
		if (connectionResult.IsFailure)
		{
			return Result.Failure(connectionResult.Error);
		}

		return ValidateForConnection(request, connectionResult.Value);
	}

	public async Task<Result<object?>> ExecuteAsync(
		Guid connectionId,
		JsonElement parameters,
		Func<double?, object?, Task> reportProgress,
		CancellationToken cancellationToken)
	{
		var request = ParseParameters(parameters);
		if (request is null)
		{
			return Result.Failure<object?>(new Error("Job.InvalidParameters", $"Valid parameters are required for '{Kind}'."));
		}

		var reporter = new DelegateProgressReporter(reportProgress);
		var result = await ExecuteTypedAsync(connectionId, request, reporter, cancellationToken);
		if (result.IsFailure)
		{
			return Result.Failure<object?>(result.Error);
		}

		object? mapped = result.Value switch
		{
			JobExecutionOutcome<TResult>.Success success => success.Value,
			JobExecutionOutcome<TResult>.PartialFailure partial => new BackgroundJobOutcome(partial.PartialResult, partial.Error, partial.CancellationRequested),
			JobExecutionOutcome<TResult>.Cancelled cancelled => new BackgroundJobCancellationOutcome(cancelled.PartialResult, cancelled.Message),
			JobExecutionOutcome<TResult>.File file => file.JobFile,
			_ => null
		};

		return Result.Success(mapped);
	}

	protected virtual IValidator<TParameters>? CreateFallbackValidator() => null;

	protected virtual Result ValidateForConnection(TParameters parameters, RedisConnection connection) => Result.Success();

	protected abstract Task<Result<JobExecutionOutcome<TResult>>> ExecuteTypedAsync(
		Guid connectionId,
		TParameters parameters,
		IJobProgressReporter<TProgress> progress,
		CancellationToken cancellationToken);

	protected virtual TParameters? ParseParameters(JsonElement parameters)
	{
		if (parameters.ValueKind != JsonValueKind.Object)
		{
			return null;
		}

		try
		{
			return parameters.Deserialize<TParameters>(JsonOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private sealed class DelegateProgressReporter(Func<double?, object?, Task> callback) : IJobProgressReporter<TProgress>
	{
		public Task ReportAsync(double? progressPercent, TProgress metrics) => callback(progressPercent, metrics);
	}
}

/// <summary>A server-owned temporary file returned through the authorized job result endpoint.</summary>
public sealed record BackgroundJobFile(string Path, string FileName, string ContentType);

/// <summary>A partial result retained when an operation fails after making changes.</summary>
public sealed record BackgroundJobOutcome(object? Result, string Error, bool CancellationRequested = false);

/// <summary>A partial result retained when an operation is cooperatively cancelled without an operational error.</summary>
public sealed record BackgroundJobCancellationOutcome(object? Result, string? Message = null);

/// <summary>Creates isolated temporary file paths for background job artifacts.</summary>
public static class BackgroundJobTempStorage
{
	public static string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "redisgui-jobs");

	public static string CreateFilePath(string prefix, string extension)
	{
		EnsureDirectoryExists();
		return Path.Combine(DirectoryPath, $"{prefix}-{Guid.NewGuid():N}{extension}");
	}

	public static void EnsureDirectoryExists()
	{
		if (Directory.Exists(DirectoryPath))
		{
			return;
		}

		if (OperatingSystem.IsWindows())
		{
			Directory.CreateDirectory(DirectoryPath);
		}
		else
		{
			Directory.CreateDirectory(
				DirectoryPath,
				UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}
	}
}
