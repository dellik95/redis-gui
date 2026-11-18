using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DeleteRedisKey;
using RedisGUI.Infrastructure.Authorization;
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
				HttpContext httpContext,
				Guid connectionId,
				string key,
				string? keyBase64,
				int? database)
			{
				var command = new DeleteRedisKeyCommand(connectionId, key, database, keyBase64);
				return await sender.Send(command).Match(
					deleted => deleted ? Results.Ok() : new Error("Redis.KeyNotFound", "The specified key was not found.").ToProblemDetails(httpContext),
					error => error.ToProblemDetails(httpContext));
			}

			routerBuilder.MapDelete("storage/{connectionId:guid}/key", (
				IMediator sender,
				HttpContext httpContext,
				Guid connectionId,
				[FromQuery] string? key = null,
				[FromQuery] string? keyBase64 = null,
				[FromQuery] int? database = null) =>
			{
				if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(keyBase64))
					return Task.FromResult(new Error("RedisValue.EmptyKey", "Redis key cannot be empty.").ToProblemDetails(httpContext));
				return HandleDelete(sender, httpContext, connectionId, key ?? string.Empty, keyBase64, database);
			}).RequireAuthorization(Policies.WriteAccess).WithName("DeleteRedisValueByQuery").WithOpenApi();

			routerBuilder.MapDelete("storage/{connectionId:guid}/{*key}", (
				IMediator sender,
				HttpContext httpContext,
				Guid connectionId,
				string key,
				[FromQuery] string? keyBase64 = null,
				[FromQuery] int? database = null) =>
				HandleDelete(sender, httpContext, connectionId, key, keyBase64, database))
				.RequireAuthorization(Policies.WriteAccess).WithOpenApi();

			routerBuilder.MapDelete("storage/{connectionId:guid}/keys/{*key}", (
				IMediator sender,
				HttpContext httpContext,
				Guid connectionId,
				string key,
				[FromQuery] string? keyBase64 = null,
				[FromQuery] int? database = null) =>
				HandleDelete(sender, httpContext, connectionId, key, keyBase64, database))
				.RequireAuthorization(Policies.WriteAccess).WithOpenApi();
		}
	}
}
