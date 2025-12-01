using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetRedisValue;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Server.Extensions;
using System;

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
		/// Returns structured error response on failure.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapGet("storage/{connectionId:guid}/{key}", async (
				IMediator sender,
				HttpContext httpContext,
				Guid connectionId,
				string key) =>
			{
				var query = new GetRedisValueQuery(connectionId, key);
				var result = await sender.Send<GetRedisValueResponse>(query);
				return result.MatchHttp(Results.Ok, httpContext);
			}).WithOpenApi();
		}
	}

}
