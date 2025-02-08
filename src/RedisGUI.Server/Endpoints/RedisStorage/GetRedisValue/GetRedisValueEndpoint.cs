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
	/// Endpoint which represents getting value from redis query.
	/// </summary>
	public class GetRedisValueEndpoint : IEndpoint
	{
		/// <inheritdoc />
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
