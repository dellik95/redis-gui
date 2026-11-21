using DispatchWeave;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DuplicateKey;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using System;

namespace RedisGUI.Server.Endpoints.RedisStorage.DuplicateKey;

public record DuplicateKeyRequest(
	string SourceKey,
	string TargetKey,
	int? Database = null,
	string? SourceKeyBase64 = null,
	string? TargetKeyBase64 = null);

public class DuplicateKeyEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/keys/duplicate", async (
			IDispatcher sender,
			Guid connectionId,
			[FromBody] DuplicateKeyRequest request) =>
		{
			var command = new DuplicateKeyCommand(
				connectionId,
				request.SourceKey,
				request.TargetKey,
				request.Database,
				request.SourceKeyBase64,
				request.TargetKeyBase64);
			return await sender.Send(command).Match(() => Results.Ok(new { Success = true }), error => error.ToProblemDetails());
		})
		.RequireAuthorization(Policies.WriteAccess)
		.WithName("DuplicateRedisKey")
		.WithSummary("Duplicate a Redis key")
		.WithDescription("Creates a complete clone of an existing Redis key with data structure and TTL intact")
		.WithTags("Redis Storage")
		.Produces(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}
