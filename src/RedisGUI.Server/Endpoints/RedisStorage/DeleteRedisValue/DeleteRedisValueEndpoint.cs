using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DeleteRedisKey;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.RedisStorage.DeleteRedisValue
{
	/// <summary>
	/// Endpoint which represents deleting value from redis command.
	/// </summary>
	public class DeleteRedisValueEndpoint : IEndpoint
	{
		/// <inheritdoc />
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapDelete("storage/{connectionId:guid}/{key}", async (
				ISender sender,
				Guid connectionId,
				string key) =>
			{
				var command = new DeleteRedisKeyCommand(connectionId, key);
				return await sender.Send(command).Match(
					deleted => deleted ? Results.Ok() : Results.NotFound(),
					Results.BadRequest);
			}).WithOpenApi();
		}
	}

}
