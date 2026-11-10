using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.RunLoadTest;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Redis;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Server.Endpoints.RedisStorage.RunLoadTest;

public class RunLoadTestEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/load-test", async (
			IMediator sender,
			Guid connectionId,
			RunLoadTestRequest? request) =>
		{
			var req = request ?? new RunLoadTestRequest();
			var command = new RunLoadTestCommand(
				connectionId,
				req.OperationsCount,
				req.Concurrency,
				req.OperationType,
				req.PayloadSizeBytes,
				req.Database,
				req.KeyPrefix,
				req.DurationSeconds);

			return await sender.Send<RedisLoadTestResult>(command).Match(
				Results.Ok,
				Results.BadRequest);
		})
		.WithName("RunLoadTest")
		.WithSummary("Run Redis load test and performance benchmark")
		.WithDescription("Executes synthetic operations against Redis to measure throughput, latency percentiles, and automatically cleans up test keys")
		.WithTags("Redis Storage")
		.Produces<RedisLoadTestResult>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}
