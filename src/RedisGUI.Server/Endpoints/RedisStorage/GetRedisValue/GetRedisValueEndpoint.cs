using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetRedisValue;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetRedisValue
{
	/// <summary>
	/// Endpoint for retrieving values from Redis storage.
	/// Fetches the value associated with a specific key.
	/// </summary>
	public class GetRedisValueEndpoint : IEndpoint
	{
		/// <summary>
		/// Maps the GET endpoint for retrieving Redis values.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
		/// <remarks>
		/// GET /storage/{connectionId}/{key}
		/// Retrieves the value for the specified key and returns OK with the value if successful, BadRequest if operation fails.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapGet("storage/{connectionId:guid}/{key}", async (
				ISender sender,
				Guid connectionId,
				string key) =>
			{
				var query = new GetRedisValueQuery(connectionId, key);
				return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
			}).WithOpenApi();
		}
	}

}
