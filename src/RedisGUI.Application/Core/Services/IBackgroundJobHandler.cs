using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Core.Services;

/// <summary>Application-owned implementation of a supported background operation.</summary>
public interface IBackgroundJobHandler
{
	string Kind { get; }
	bool RequiresWriteAccess { get; }
	Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken);
	Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters,
		Func<double?, object?, Task> reportProgress, CancellationToken cancellationToken);
}

/// <summary>A server-owned temporary file returned through the authorized job result endpoint.</summary>
public sealed record BackgroundJobFile(string Path, string FileName, string ContentType);
