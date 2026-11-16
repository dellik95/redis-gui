using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
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

/// <summary>Starts, checks, cancels, and retrieves results for owned background jobs.</summary>
public sealed class BackgroundJobsEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("jobs", async (StartBackgroundJobRequest request, HttpContext context, ICurrentUserService currentUser,
			IEnumerable<IBackgroundJobHandler> handlers, BackgroundJobManager jobs, IAuthorizationService authorization, CancellationToken cancellationToken) =>
		{
			if (currentUser.UserId is not Guid ownerId) return Results.Forbid();
			if (request.Parameters.ValueKind != JsonValueKind.Object)
				return Results.BadRequest(new { message = "Job parameters must be an object." });
			var handler = handlers.SingleOrDefault(item => item.Kind == request.Kind);
			if (handler is null) return Results.BadRequest(new { message = "Unknown job kind." });
			if (request.Parameters.GetRawText().Length > handler.MaxParameterCharacters)
				return Results.BadRequest(new { message = "Job parameters exceed the limit for this operation." });
			if (handler.RequiresWriteAccess && !(await authorization.AuthorizeAsync(context.User, Policies.WriteAccess)).Succeeded) return Results.Forbid();
			var validation = await handler.ValidateAsync(request.ConnectionId, request.Parameters, cancellationToken);
			if (validation.IsFailure) return validation.Error.ToProblemDetails(context);
			if (!jobs.TryStart(ownerId, context.User, request.ConnectionId, request.Kind, request.Parameters, out var snapshot))
				return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "The job queue is full. Try again later.");
			return Results.Accepted($"/api/v1/jobs/{snapshot!.JobId}", snapshot);
		}).RequireAuthorization(Policies.Authenticated).RequireRateLimiting("sensitive").WithName("StartBackgroundJob").WithTags("Jobs");

		routerBuilder.MapGet("jobs/{jobId:guid}", async (Guid jobId, ICurrentUserService user, BackgroundJobManager jobs,
			IRedisConnectionRepository repository, IConnectionAccessService access, CancellationToken token) =>
		{
			var job = await AuthorizedJob(jobId, user, jobs, repository, access, token);
			return job is null ? Results.NotFound() : Results.Ok(job);
		}).RequireAuthorization(Policies.Authenticated).WithName("GetBackgroundJob").WithTags("Jobs");

		routerBuilder.MapPost("jobs/{jobId:guid}/cancel", async (Guid jobId, ICurrentUserService user, BackgroundJobManager jobs,
			IRedisConnectionRepository repository, IConnectionAccessService access, CancellationToken token) =>
		{
			var job = await AuthorizedJob(jobId, user, jobs, repository, access, token);
			if (job is null || user.UserId is not Guid ownerId) return Results.NotFound();
			await jobs.CancelAsync(jobId, ownerId);
			return Results.Accepted($"/api/v1/jobs/{jobId}", jobs.Get(jobId, ownerId));
		}).RequireAuthorization(Policies.Authenticated).WithName("CancelBackgroundJob").WithTags("Jobs");

		routerBuilder.MapGet("jobs/{jobId:guid}/result", async (Guid jobId, ICurrentUserService user, BackgroundJobManager jobs,
			IRedisConnectionRepository repository, IConnectionAccessService access, CancellationToken token) =>
		{
			var job = await AuthorizedJob(jobId, user, jobs, repository, access, token);
			if (job is null || user.UserId is not Guid ownerId) return Results.NotFound();
			if (!job.HasResult) return Results.Conflict(new { message = "A result is not available for this job." });
			var result = jobs.GetResult(jobId, ownerId);
			return result is BackgroundJobFile file
				? Results.File(file.Path, file.ContentType, file.FileName, enableRangeProcessing: true)
				: Results.Ok(result);
		}).RequireAuthorization(Policies.Authenticated).WithName("GetBackgroundJobResult").WithTags("Jobs");
	}

	private static async System.Threading.Tasks.Task<BackgroundJobSnapshot?> AuthorizedJob(Guid id, ICurrentUserService user, BackgroundJobManager jobs,
		IRedisConnectionRepository repository, IConnectionAccessService access, CancellationToken token)
	{
		var job = user.UserId is Guid ownerId ? jobs.Get(id, ownerId) : null;
		if (job is null) return null;
		var connection = await repository.GetConnectionByIdAsync(job.ConnectionId, token);
		return connection.IsSuccess && await access.CanAccessAsync(connection.Value) ? job : null;
	}
}
