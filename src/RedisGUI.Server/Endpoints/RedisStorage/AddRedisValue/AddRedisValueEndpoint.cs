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
	/// Endpoint which represents adding value to redis command.
	/// </summary>
	public class AddRedisValueEndpoint : IEndpoint
	{
		/// <inheritdoc />
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
