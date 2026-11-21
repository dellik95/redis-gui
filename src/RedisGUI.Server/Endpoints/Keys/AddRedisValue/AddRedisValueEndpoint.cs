using DispatchWeave;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.UpdateRedisValue;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Server.Endpoints.RedisStorage.UpdateRedisValue;
using System;
using System.Threading.Tasks;

namespace RedisGUI.Server.Endpoints.RedisStorage.AddRedisValue
{
	/// <summary>
	/// Endpoint for adding new keys or updating values in Redis storage.
	/// Supports all Redis data types (String, List, Hash, Set, SortedSet, Stream).
	/// </summary>
	public class AddRedisValueEndpoint : IEndpoint
	{
		/// <summary>
		/// Maps the POST endpoints for adding Redis values.
		/// </summary>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			async Task<IResult> HandleAddKey(
				IDispatcher sender,
				HttpContext httpContext,
				Guid connectionId,
				UpdateRedisValueRequest request,
				int? database = null)
			{
				TimeSpan? ttl = request.Ttl.HasValue && request.Ttl.Value > 0
					? TimeSpan.FromSeconds(request.Ttl.Value)
					: null;

				var command = new UpdateRedisValueCommand(
					connectionId,
					request.Key,
					request.Type,
					request.Value,
					ttl,
					database ?? request.Database,
					request.KeyBase64);

				return await sender.Send(command).Match(
					() => Results.Ok(),
					error => error.ToProblemDetails(httpContext));
			}

			routerBuilder.MapPost("storage/{connectionId:guid}", HandleAddKey)
				.RequireAuthorization(Policies.WriteAccess)
				.WithName("AddRedisValue")
				.WithSummary("Add a new Redis key-value pair")
				.WithDescription("Creates a new Redis key with specified type and value")
				.WithOpenApi();

			routerBuilder.MapPost("storage/{connectionId:guid}/keys", HandleAddKey)
				.RequireAuthorization(Policies.WriteAccess)
				.WithName("AddRedisValueKeys")
				.WithSummary("Add a new Redis key-value pair (keys subpath)")
				.WithOpenApi();
		}
	}
}
