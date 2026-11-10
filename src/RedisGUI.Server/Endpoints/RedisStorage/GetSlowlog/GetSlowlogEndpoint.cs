using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetSlowlog;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Redis;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Collections.Generic;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetSlowlog;

public class GetSlowlogEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("storage/{connectionId:guid}/slowlog", async (
			IMediator sender,
			Guid connectionId,
			int count = 25) =>
		{
			var query = new GetSlowlogQuery(connectionId, count);
			return await sender.Send<List<SlowlogEntryInfo>>(query).Match(Results.Ok, Results.BadRequest);
		})
		.WithName("GetSlowlog")
		.WithSummary("Get Redis slowlog entries")
		.WithDescription("Retrieves the execution slowlog of operations exceeding the slowlog threshold")
		.WithTags("Redis Storage")
		.Produces<List<SlowlogEntryInfo>>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}
