using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.Jobs;

public sealed record StartBackgroundJobRequest(string Kind, Guid ConnectionId, JsonElement Parameters);

public sealed record BackgroundJobListItem(
	Guid JobId,
	Guid ConnectionId,
	string Kind,
	string Status,
	long Revision,
	double? ProgressPercent,
	object? Metrics,
	string? Error,
	bool HasResult,
	DateTime UpdatedAt,
	DateTime? CreatedAt,
	DateTime? StartedAt,
	DateTime? CompletedAt,
	bool CancellationRequested,
	bool MayHaveUnconfirmedChanges,
	string? DisplayName,
	string? ConnectionName);

public sealed record BackgroundJobKindInfo(
	string Kind,
	string DisplayName,
	bool RequiresWriteAccess,
	int MaxParameterCharacters);

/// <summary>Starts, lists, checks, cancels, and retrieves results for owned background jobs.</summary>
public sealed class BackgroundJobsEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("jobs/kinds", (IEnumerable<IBackgroundJobHandler> handlers) =>
		{
			var list = handlers
				.Select(h => new BackgroundJobKindInfo(h.Kind, h.DisplayName, h.RequiresWriteAccess, h.MaxParameterCharacters))
				.OrderBy(h => h.Kind, StringComparer.Ordinal)
				.ToArray();
			return Results.Ok(list);
		}).RequireAuthorization(Policies.Authenticated).WithName("GetBackgroundJobKinds").WithTags("Jobs");

		routerBuilder.MapGet("jobs", async (
			Guid? connectionId,
			string? status,
			ICurrentUserService currentUser,
			BackgroundJobManager jobs,
			IRedisConnectionRepository repository,
			IConnectionAccessService access,
			CancellationToken token) =>
		{
			if (currentUser.UserId is not Guid ownerId) return Results.Forbid();

			var snapshots = jobs.ListForOwner(ownerId);
			var accessCache = new Dictionary<Guid, (bool Allowed, string? Name)>();
			var authorized = new List<BackgroundJobListItem>(snapshots.Count);

			foreach (var job in snapshots)
			{
				if (connectionId.HasValue && job.ConnectionId != connectionId.Value) continue;
				if (!string.IsNullOrWhiteSpace(status) && !string.Equals(job.Status, status, StringComparison.OrdinalIgnoreCase)) continue;

				if (!accessCache.TryGetValue(job.ConnectionId, out var connAccess))
				{
					var connResult = await repository.GetConnectionByIdAsync(job.ConnectionId, token);
					bool allowed = connResult.IsSuccess && await access.CanAccessAsync(connResult.Value);
					string? name = connResult.IsSuccess ? connResult.Value.ConnectionName?.Value : null;
					connAccess = (allowed, name);
					accessCache[job.ConnectionId] = connAccess;
				}

				if (!connAccess.Allowed) continue;

				authorized.Add(new BackgroundJobListItem(
					job.JobId,
					job.ConnectionId,
					job.Kind,
					job.Status,
					job.Revision,
					job.ProgressPercent,
					job.Metrics,
					job.Error,
					job.HasResult,
					job.UpdatedAt,
					job.CreatedAt,
					job.StartedAt,
					job.CompletedAt,
					job.CancellationRequested,
					job.MayHaveUnconfirmedChanges,
					job.DisplayName,
					connAccess.Name));
			}

			return Results.Ok(authorized);
		}).RequireAuthorization(Policies.Authenticated).WithName("ListBackgroundJobs").WithTags("Jobs");

		routerBuilder.MapPost("jobs", async (
			StartBackgroundJobRequest request,
			HttpContext context,
			ICurrentUserService currentUser,
			IEnumerable<IBackgroundJobHandler> handlers,
			BackgroundJobManager jobs,
			IAuthorizationService authorization,
			CancellationToken cancellationToken) =>
		{
			if (currentUser.UserId is not Guid ownerId) return Results.Forbid();
			if (request.Parameters.ValueKind != JsonValueKind.Object)
				return Results.BadRequest(new { message = "Job parameters must be an object." });

			var handler = handlers.SingleOrDefault(item => item.Kind == request.Kind);
			if (handler is null) return Results.BadRequest(new { message = "Unknown job kind." });
			if (request.Parameters.GetRawText().Length > handler.MaxParameterCharacters)
				return Results.BadRequest(new { message = "Job parameters exceed the limit for this operation." });
			if (handler.RequiresWriteAccess && !(await authorization.AuthorizeAsync(context.User, Policies.WriteAccess)).Succeeded)
				return Results.Forbid();

			var validation = await handler.ValidateAsync(request.ConnectionId, request.Parameters, cancellationToken);
			if (validation.IsFailed) return validation.GetDomainError().ToProblemDetails(context);

			if (!jobs.TryStart(ownerId, context.User, request.ConnectionId, request.Kind, request.Parameters, out var snapshot, handler.DisplayName))
			{
				return Results.Problem(
					statusCode: StatusCodes.Status503ServiceUnavailable,
					detail: "The job queue is full. Try again later.");
			}

			return Results.Accepted($"/api/v1/jobs/{snapshot!.JobId}", snapshot);
		}).RequireAuthorization(Policies.Authenticated).RequireRateLimiting("sensitive").WithName("StartBackgroundJob").WithTags("Jobs");

		routerBuilder.MapGet("jobs/{jobId:guid}", async (
			Guid jobId,
			ICurrentUserService user,
			BackgroundJobManager jobs,
			IRedisConnectionRepository repository,
			IConnectionAccessService access,
			CancellationToken token) =>
		{
			var job = await AuthorizedJob(jobId, user, jobs, repository, access, token);
			return job is null ? Results.NotFound() : Results.Ok(job);
		}).RequireAuthorization(Policies.Authenticated).WithName("GetBackgroundJob").WithTags("Jobs");

		routerBuilder.MapPost("jobs/{jobId:guid}/cancel", async (
			Guid jobId,
			ICurrentUserService user,
			BackgroundJobManager jobs,
			IRedisConnectionRepository repository,
			IConnectionAccessService access,
			CancellationToken token) =>
		{
			var job = await AuthorizedJob(jobId, user, jobs, repository, access, token);
			if (job is null || user.UserId is not Guid ownerId) return Results.NotFound();
			await jobs.CancelAsync(jobId, ownerId);
			return Results.Accepted($"/api/v1/jobs/{jobId}", jobs.Get(jobId, ownerId));
		}).RequireAuthorization(Policies.Authenticated).WithName("CancelBackgroundJob").WithTags("Jobs");

		routerBuilder.MapGet("jobs/{jobId:guid}/result", async (
			Guid jobId,
			HttpContext context,
			ICurrentUserService user,
			BackgroundJobManager jobs,
			IRedisConnectionRepository repository,
			IConnectionAccessService access,
			CancellationToken token) =>
		{
			var job = await AuthorizedJob(jobId, user, jobs, repository, access, token);
			if (job is null || user.UserId is not Guid ownerId) return Results.NotFound();
			if (!job.HasResult) return Results.Conflict(new { message = "A result is not available for this job." });

			if (jobs.TryPinResultFile(jobId, ownerId, out var file, out var downloadLease) && file is not null && downloadLease is not null)
			{
				context.Response.RegisterForDispose(downloadLease);
				return Results.File(file.Path, file.ContentType, file.FileName, enableRangeProcessing: true);
			}

			var result = jobs.GetResult(jobId, ownerId);
			return result is BackgroundJobFile
				? Results.NotFound(new { message = "The job result file has expired." })
				: Results.Ok(result);
		}).RequireAuthorization(Policies.Authenticated).WithName("GetBackgroundJobResult").WithTags("Jobs");
	}

	private static async Task<BackgroundJobSnapshot?> AuthorizedJob(
		Guid id,
		ICurrentUserService user,
		BackgroundJobManager jobs,
		IRedisConnectionRepository repository,
		IConnectionAccessService access,
		CancellationToken token)
	{
		var job = user.UserId is Guid ownerId ? jobs.Get(id, ownerId) : null;
		if (job is null) return null;
		var connection = await repository.GetConnectionByIdAsync(job.ConnectionId, token);
		return connection.IsSuccess && await access.CanAccessAsync(connection.Value) ? job : null;
	}
}
