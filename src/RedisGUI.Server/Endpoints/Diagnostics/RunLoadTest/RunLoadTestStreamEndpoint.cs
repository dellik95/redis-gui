using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.RunLoadTest;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;

namespace RedisGUI.Server.Endpoints.RedisStorage.RunLoadTest;

public class RunLoadTestStreamEndpoint : IEndpoint
{
	private static readonly ConcurrentDictionary<string, (Guid ConnectionId, Guid UserId, CancellationTokenSource Source)> ActiveLoadTests = new();

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/load-test/stream", async (
			HttpContext httpContext,
			IMediator mediator,
			ICurrentUserService currentUser,
			Guid connectionId,
			RunLoadTestRequest? request,
			[FromQuery] string? testId) =>
		{
			var req = request ?? new RunLoadTestRequest();
			var id = string.IsNullOrWhiteSpace(testId) ? Guid.NewGuid().ToString("N") : testId;
			if (currentUser.UserId is not Guid userId)
			{
				await Results.Forbid().ExecuteAsync(httpContext);
				return;
			}

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
			if (!ActiveLoadTests.TryAdd(id, (connectionId, userId, cts)))
			{
				await Results.Conflict(new { message = "A load test with this ID is already running." }).ExecuteAsync(httpContext);
				return;
			}

			httpContext.Response.ContentType = "text/event-stream";
			httpContext.Response.Headers.Append("Cache-Control", "no-cache");
			httpContext.Response.Headers.Append("Connection", "keep-alive");

			try
			{
				var domainReq = new RedisLoadTestRequest(
					req.OperationsCount,
					req.Concurrency,
					req.OperationType,
					req.PayloadSizeBytes,
					req.Database,
					req.KeyPrefix,
					req.DurationSeconds);

				var command = new RunStreamingLoadTestCommand(
					connectionId,
					domainReq,
					async (progress) =>
					{
						try
						{
							var json = JsonSerializer.Serialize(progress, JsonOptions);
							await httpContext.Response.WriteAsync($"event: progress\ndata: {json}\n\n");
							await httpContext.Response.Body.FlushAsync();
						}
						catch
						{
							// Ignore client disconnect during progress emission
						}
					});
				var result = await mediator.Send<RedisLoadTestResult>(command, cts.Token);

				if (result.IsSuccess)
				{
					var resultJson = JsonSerializer.Serialize(result.Value, JsonOptions);
					await httpContext.Response.WriteAsync($"event: complete\ndata: {resultJson}\n\n");
					await httpContext.Response.Body.FlushAsync();
				}
				else
				{
					if (!httpContext.Response.HasStarted)
					{
						await result.Error.ToProblemDetails(httpContext).ExecuteAsync(httpContext);
						return;
					}
					var errJson = JsonSerializer.Serialize(new { error = result.Error.Message }, JsonOptions);
					await httpContext.Response.WriteAsync($"event: error\ndata: {errJson}\n\n");
					await httpContext.Response.Body.FlushAsync();
				}
			}
			catch (OperationCanceledException)
			{
				// Client cancelled or user stopped benchmark
			}
			catch (Exception ex)
			{
				try
				{
					var errJson = JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
					await httpContext.Response.WriteAsync($"event: error\ndata: {errJson}\n\n");
					await httpContext.Response.Body.FlushAsync();
				}
				catch { }
			}
			finally
			{
				ActiveLoadTests.TryRemove(id, out _);
			}
		})
		.WithName("RunLoadTestStream")
		.WithSummary("Run Redis load test with SSE real-time streaming progress and duration")
		.WithTags("Redis Storage")
		.RequireAuthorization(Policies.WriteAccess);

		routerBuilder.MapPost("storage/{connectionId:guid}/load-test/stop", (
			Guid connectionId, ICurrentUserService currentUser, [FromBody] StopLoadTestRequest req) =>
		{
			if (!string.IsNullOrWhiteSpace(req.TestId) && ActiveLoadTests.TryGetValue(req.TestId, out var test)
				&& test.ConnectionId == connectionId && test.UserId == currentUser.UserId)
			{
				test.Source.Cancel();
				return Results.Ok(new { success = true, message = "Load test stopped" });
			}
			return Results.Ok(new { success = false, message = "Test not found or already completed" });
		})
		.WithName("StopLoadTest")
		.WithSummary("Stop an active Redis load test session")
		.WithTags("Redis Storage")
		.RequireAuthorization(Policies.WriteAccess);
	}
}

public class StopLoadTestRequest
{
	public string TestId { get; set; } = string.Empty;
}
