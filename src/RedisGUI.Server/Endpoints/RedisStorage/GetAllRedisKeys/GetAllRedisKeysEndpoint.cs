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
	/// Endpoint which represents getting all keys from redis query.
	/// </summary>
	public class GetAllRedisKeysEndpoint : IEndpoint
	{
		/// <inheritdoc />
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
