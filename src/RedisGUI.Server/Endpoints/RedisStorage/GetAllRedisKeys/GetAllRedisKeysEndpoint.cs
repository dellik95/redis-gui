using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetAllRedisValues;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetAllRedisKeys
{
	/// <summary>
	/// Endpoint for retrieving all keys from Redis storage.
	/// Supports pattern matching and pagination.
	/// </summary>
	public class GetAllRedisKeysEndpoint : IEndpoint
	{
		/// <summary>
		/// Maps the GET endpoint for retrieving all Redis keys.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
		/// <remarks>
		/// GET /storage/{connectionId}
		/// Retrieves all keys matching the specified pattern with pagination support.
		/// Returns OK with the keys if successful, BadRequest if operation fails.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapGet("storage/{connectionId:guid}", async (
				ISender sender,
				Guid connectionId,
				string pattern,
				int? pageSize,
				int? pageNumber) =>
			{
				var query = new GetAllRedisKeysQuery(connectionId, pattern, pageSize, pageNumber);
				return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
			}).WithOpenApi();
		}
	}
}
