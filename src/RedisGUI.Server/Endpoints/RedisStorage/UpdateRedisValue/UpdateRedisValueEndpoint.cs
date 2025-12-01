using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.UpdateRedisValue;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Server.Endpoints.RedisStorage.UpdateRedisValue;

/// <summary>
/// Endpoint for updating Redis values with type-specific logic
/// </summary>
public class UpdateRedisValueEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for updating Redis values
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPut("storage/{connectionId:guid}/keys/{key}", async (
			IMediator sender,
			Guid connectionId,
			string key,
			UpdateRedisValueRequest request) =>
		{
			var command = new UpdateRedisValueCommand(
				connectionId,
				key,
				request.Type,
				request.Value,
				request.Ttl);

			return await sender.Send(command).Match(() => Results.Ok(), Results.BadRequest);
		})
		.WithName("UpdateRedisValue")
		.WithSummary("Update a Redis value")
		.WithDescription("Updates a Redis value based on its type (String, List, Set, Hash, SortedSet, Stream)");
	}
}
