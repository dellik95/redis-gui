using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DuplicateKey;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Server.Endpoints.RedisStorage.DuplicateKey;

public record DuplicateKeyRequest(string SourceKey, string TargetKey, int? Database = null);

public class DuplicateKeyEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/keys/duplicate", async (
			IMediator sender,
			Guid connectionId,
			[FromBody] DuplicateKeyRequest request) =>
		{
			var command = new DuplicateKeyCommand(connectionId, request.SourceKey, request.TargetKey, request.Database);
			return await sender.Send(command).Match(() => Results.Ok(new { Success = true }), Results.BadRequest);
		})
		.WithName("DuplicateRedisKey")
		.WithSummary("Duplicate a Redis key")
		.WithDescription("Creates a complete clone of an existing Redis key with data structure and TTL intact")
		.WithTags("Redis Storage")
		.Produces(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}
