using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.BulkUpdateTtl;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Domain.Connection;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RedisGUI.Server.Endpoints.RedisStorage.BulkUpdateTtl;

public record BulkUpdateTtlRequest(List<string> Keys, long? TtlSeconds, int? Database = null);

public class BulkUpdateTtlEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/keys/bulk-ttl", async (
			IMediator sender,
			Guid connectionId,
			[FromBody] BulkUpdateTtlRequest request, CancellationToken cancellationToken) =>
		{
			var command = new BulkUpdateTtlCommand(connectionId, request.Keys, request.TtlSeconds, request.Database);
			return await sender.Send<RedisBulkTtlResult>(command, cancellationToken).Match(
				Results.Ok,
				error => error.ToProblemDetails());
		})
		.WithName("BulkUpdateTtl")
		.RequireAuthorization(Policies.WriteAccess)
		.WithSummary("Bulk update TTL or persist keys")
		.WithTags("Redis Storage")
		.Produces(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}
