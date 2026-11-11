using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DeleteRedisKey;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Threading.Tasks;

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
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			async Task<IResult> HandleDelete(
				IMediator sender,
				Guid connectionId,
				string key,
				int? database)
			{
				var command = new DeleteRedisKeyCommand(connectionId, key, database);
				return await sender.Send(command).Match(
					deleted => deleted ? Results.Ok() : Results.NotFound(),
					Results.BadRequest);
			}

			routerBuilder.MapDelete("storage/{connectionId:guid}/{*key}", HandleDelete).WithOpenApi();
			routerBuilder.MapDelete("storage/{connectionId:guid}/keys/{*key}", HandleDelete).WithOpenApi();
		}
	}
}
