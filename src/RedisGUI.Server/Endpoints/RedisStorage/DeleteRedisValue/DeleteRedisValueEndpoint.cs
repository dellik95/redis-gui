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
	/// Endpoint for deleting values from Redis storage.
	/// Removes key-value pairs from the Redis database.
	/// </summary>
	public class DeleteRedisValueEndpoint : IEndpoint
	{
		/// <summary>
		/// Maps the DELETE endpoint for removing Redis values.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
		/// <remarks>
		/// DELETE /storage/{connectionId}/{key}
		/// Deletes the specified key-value pair and returns OK if successful, NotFound if key doesn't exist, or BadRequest if operation fails.
		/// </remarks>
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
