using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Server.Abstraction;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Server.Endpoints.RedisStorage.RunLoadTest;

public class RunLoadTestStreamEndpoint : IEndpoint
{
	private static readonly ConcurrentDictionary<string, CancellationTokenSource> ActiveLoadTests = new();

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/load-test/stream", async (
			HttpContext httpContext,
			IRedisConnectionRepository connectionRepo,
			IRedisLoadTestService loadTestService,
			Guid connectionId,
			RunLoadTestRequest? request,
			[FromQuery] string? testId) =>
		{
			var req = request ?? new RunLoadTestRequest();
			var id = string.IsNullOrWhiteSpace(testId) ? Guid.NewGuid().ToString("N") : testId;

			var connectionResult = await connectionRepo.GetConnectionByIdAsync(connectionId, httpContext.RequestAborted);
			if (connectionResult.IsFailure)
			{
				httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
				await httpContext.Response.WriteAsJsonAsync(new { error = connectionResult.Error.Message });
				return;
			}

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
			ActiveLoadTests[id] = cts;

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

				var result = await loadTestService.RunStreamingLoadTestAsync(
					connectionResult.Value,
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
					},
					cts.Token);

				if (result.IsSuccess)
				{
					var resultJson = JsonSerializer.Serialize(result.Value, JsonOptions);
					await httpContext.Response.WriteAsync($"event: complete\ndata: {resultJson}\n\n");
					await httpContext.Response.Body.FlushAsync();
				}
				else
				{
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
		.WithTags("Redis Storage");

		routerBuilder.MapPost("storage/{connectionId:guid}/load-test/stop", (
			[FromBody] StopLoadTestRequest req) =>
		{
			if (!string.IsNullOrWhiteSpace(req.TestId) && ActiveLoadTests.TryGetValue(req.TestId, out var cts))
			{
				cts.Cancel();
				return Results.Ok(new { success = true, message = "Load test stopped" });
			}
			return Results.Ok(new { success = false, message = "Test not found or already completed" });
		})
		.WithName("StopLoadTest")
		.WithSummary("Stop an active Redis load test session")
		.WithTags("Redis Storage");
	}
}

public class StopLoadTestRequest
{
	public string TestId { get; set; } = string.Empty;
}
