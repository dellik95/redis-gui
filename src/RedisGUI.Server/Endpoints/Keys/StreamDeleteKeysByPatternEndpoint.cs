using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Redis;

/// <summary>Streams confirmed deletion counts and supports an explicit stop request.</summary>
public sealed class StreamDeleteKeysByPatternEndpoint : IEndpoint
{
	private static readonly ConcurrentDictionary<Guid, (Guid ConnectionId, Guid UserId, CancellationTokenSource Source)> Operations = new();
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/keys/pattern-delete/stream", async (
			Guid connectionId, PatternDeleteRequest request, HttpContext context, ICurrentUserService currentUser, IMediator mediator) =>
		{
			if (currentUser.UserId is not Guid userId) { await Results.Forbid().ExecuteAsync(context); return; }
			using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
			if (request.OperationId == Guid.Empty || !Operations.TryAdd(request.OperationId, (connectionId, userId, cancellation)))
			{
				await Results.Conflict(new { message = "Operation ID is missing or already in use." }).ExecuteAsync(context);
				return;
			}
			context.Response.ContentType = "text/event-stream";
			context.Response.Headers.CacheControl = "no-cache";
			var clock = Stopwatch.StartNew();
			try
			{
				var command = new StreamDeleteKeysByPatternCommand(connectionId, request.Pattern, request.Database, async count =>
				{
					if (count != 1 && clock.ElapsedMilliseconds < 250) return;
					clock.Restart();
					try
					{
						await context.Response.WriteAsync($"event: progress\ndata: {JsonSerializer.Serialize(new { deletedCount = count }, JsonOptions)}\n\n", context.RequestAborted);
						await context.Response.Body.FlushAsync(context.RequestAborted);
					}
					catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
				});
				var result = await mediator.Send<StreamDeleteKeysByPatternResult>(command, cancellation.Token);
				if (result.IsFailure && !context.Response.HasStarted)
				{
					await result.Error.ToProblemDetails(context).ExecuteAsync(context);
					return;
				}
				var payload = result.IsSuccess ? result.Value : new StreamDeleteKeysByPatternResult(0, false, result.Error.Message);
				await context.Response.WriteAsync($"event: complete\ndata: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n", context.RequestAborted);
				await context.Response.Body.FlushAsync(context.RequestAborted);
			}
			catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested && !context.Response.HasStarted)
			{
				var payload = new StreamDeleteKeysByPatternResult(0, true, null);
				await context.Response.WriteAsync($"event: complete\ndata: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n", context.RequestAborted);
			}
			finally { Operations.TryRemove(request.OperationId, out _); }
		}).RequireAuthorization(Policies.WriteAccess).WithName("StreamDeleteKeysByPattern").WithTags("Redis Storage");

		routerBuilder.MapPost("storage/{connectionId:guid}/keys/pattern-delete/stop", (
			Guid connectionId, StopPatternDeleteRequest request, ICurrentUserService currentUser) =>
		{
			if (!Operations.TryGetValue(request.OperationId, out var operation) || operation.ConnectionId != connectionId || operation.UserId != currentUser.UserId)
				return Results.NotFound();
			try { operation.Source.Cancel(); return Results.Ok(); }
			catch (ObjectDisposedException) { return Results.NotFound(); }
		}).RequireAuthorization(Policies.WriteAccess).WithName("StopDeleteKeysByPattern").WithTags("Redis Storage");
	}
}

public sealed record PatternDeleteRequest(Guid OperationId, string Pattern, int Database);
public sealed record StopPatternDeleteRequest(Guid OperationId);
