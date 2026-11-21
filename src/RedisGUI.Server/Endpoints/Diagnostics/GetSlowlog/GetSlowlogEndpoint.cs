using DispatchWeave;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetSlowlog;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using System;
using System.Collections.Generic;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetSlowlog;

public class GetSlowlogEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("storage/{connectionId:guid}/slowlog", async (
			IDispatcher sender,
			Guid connectionId,
			int count = 25) =>
		{
			var query = new GetSlowlogQuery(connectionId, count);
			return await sender.Send(query).Match(Results.Ok, error => error.ToProblemDetails());
		})
		.RequireAuthorization(Policies.Authenticated)
		.WithName("GetSlowlog")
		.WithSummary("Get Redis slowlog entries")
		.WithDescription("Retrieves the execution slowlog of operations exceeding the slowlog threshold")
		.WithTags("Redis Storage")
		.Produces<List<SlowlogEntryInfo>>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}
