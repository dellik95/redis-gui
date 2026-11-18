using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetKeyMemoryUsage;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetKeyMemoryUsage;

/// <summary>
/// Endpoint for retrieving memory usage of a specific Redis key
/// </summary>
public sealed class GetKeyMemoryUsageEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("storage/{connectionId:guid}/key/memory", async (
			IMediator sender,
			Guid connectionId,
			[FromQuery] string? key = null,
			[FromQuery] string? keyBase64 = null,
			[FromQuery] int? database = null,
			CancellationToken cancellationToken = default) =>
		{
			GetKeyMemoryUsageQuery query = new(connectionId, key ?? string.Empty, database, keyBase64);
			return await sender.Send<GetKeyMemoryUsageResponse>(query, cancellationToken)
				.Match(Results.Ok, error => error.ToProblemDetails());
		})
		.WithName("GetKeyMemoryUsageByQuery")
		.WithSummary("Get key memory usage in bytes via query parameters")
		.WithTags("Redis Storage")
		.Produces<GetKeyMemoryUsageResponse>(StatusCodes.Status200OK)
		.RequireAuthorization(Policies.Authenticated);

		routerBuilder.MapGet("storage/{connectionId:guid}/keys/{key}/memory", async (
			IMediator sender,
			Guid connectionId,
			string key,
			[FromQuery] int? database = null,
			[FromQuery] string? keyBase64 = null,
			CancellationToken cancellationToken = default) =>
		{
			GetKeyMemoryUsageQuery query = new(connectionId, key, database, keyBase64);
			return await sender.Send<GetKeyMemoryUsageResponse>(query, cancellationToken)
				.Match(Results.Ok, error => error.ToProblemDetails());
		})
		.WithName("GetKeyMemoryUsage")
		.WithSummary("Get key memory usage in bytes")
		.WithDescription("Retrieves the memory footprint in bytes for a specific Redis key using MEMORY USAGE")
		.WithTags("Redis Storage")
		.Produces<GetKeyMemoryUsageResponse>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status404NotFound)
		.RequireAuthorization(Policies.Authenticated);
	}
}
