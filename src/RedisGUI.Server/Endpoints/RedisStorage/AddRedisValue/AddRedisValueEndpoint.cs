using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.SetRedisValue;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.RedisStorage.AddRedisValue
{
	/// <summary>
	/// Endpoint for adding or updating values in Redis storage.
	/// Handles setting key-value pairs with optional TTL.
	/// </summary>
	public class AddRedisValueEndpoint : IEndpoint
	{
		/// <summary>
		/// Maps the POST endpoint for adding Redis values.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
		/// <remarks>
		/// POST /storage/{connectionId}
		/// Adds or updates a Redis key-value pair and returns OK if successful, BadRequest if operation fails.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapPost("storage/{connectionId:guid}", async (
				ISender sender,
				Guid connectionId,
				AddRedisValueRequest request) =>
			{
				var command = new AddRedisValueCommand(
					connectionId,
					request.Key,
					request.Value,
					request.Ttl);

				return await sender.Send(command).Match(() => Results.Ok(), Results.BadRequest);

			}).WithOpenApi();
		}
	}
}
