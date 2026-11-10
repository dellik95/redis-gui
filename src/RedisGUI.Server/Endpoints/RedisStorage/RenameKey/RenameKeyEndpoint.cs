using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.RenameKey;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Server.Endpoints.RedisStorage.RenameKey;

public record RenameKeyRequest(string OldKey, string NewKey, bool Overwrite = false, int? Database = null);

public class RenameKeyEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/keys/rename", async (
			IMediator sender,
			Guid connectionId,
			[FromBody] RenameKeyRequest request) =>
		{
			var command = new RenameKeyCommand(connectionId, request.OldKey, request.NewKey, request.Overwrite, request.Database);
			return await sender.Send(command).Match(() => Results.Ok(new { Success = true }), Results.BadRequest);
		})
		.WithName("RenameRedisKey")
		.WithSummary("Rename a Redis key")
		.WithTags("Redis Storage")
		.Produces(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}
