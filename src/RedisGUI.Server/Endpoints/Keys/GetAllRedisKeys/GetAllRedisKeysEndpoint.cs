using DispatchWeave;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetAllRedisValues;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using System;
using System.Threading;

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
				IDispatcher sender,
				Guid connectionId,
				string? pattern,
				int? pageSize,
				int? pageNumber,
				int? database,
				CancellationToken cancellationToken) =>
			{
				GetAllRedisKeysQuery query = new(connectionId, pattern ?? "*", pageSize, pageNumber, database);
				var result = await sender.Send(query, cancellationToken);
				return result.ToHttpResult();
			})
			.WithName("GetAllRedisKeys")
			.WithSummary("Get all Redis keys")
			.WithDescription("Retrieves all keys from Redis storage with optional pattern matching and pagination support")
			.WithTags("Redis Storage")
			.Produces<GetAllRedisValuesResponse>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization(Policies.Authenticated);
		}
	}
}
