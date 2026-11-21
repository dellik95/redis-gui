using DispatchWeave;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetRedisValue;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Server.Extensions;
using System;
using System.Threading.Tasks;

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
		/// GET /storage/{connectionId}/key?key=...&amp;keyBase64=... or GET /storage/{connectionId}/{key}
		/// Returns structured error response on failure.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			async Task<IResult> HandleGet(
				IDispatcher sender,
				HttpContext httpContext,
				Guid connectionId,
				string key,
				string? keyBase64,
				int? database)
			{
				var query = new GetRedisValueQuery(connectionId, key, database, keyBase64);
				var result = await sender.Send(query);
				return result.MatchHttp(Results.Ok, httpContext);
			}

			routerBuilder.MapGet("storage/{connectionId:guid}/key", (
				IDispatcher sender,
				HttpContext httpContext,
				Guid connectionId,
				[FromQuery] string? key = null,
				[FromQuery] string? keyBase64 = null,
				[FromQuery] int? database = null) =>
			{
				if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(keyBase64))
					return Task.FromResult(new DomainError("RedisValue.EmptyKey", "Redis key cannot be empty.").ToProblemDetails(httpContext));
				return HandleGet(sender, httpContext, connectionId, key ?? string.Empty, keyBase64, database);
			}).RequireAuthorization(Policies.Authenticated).WithName("GetRedisValueByQuery").WithOpenApi();

			routerBuilder.MapGet("storage/{connectionId:guid}/{*key}", (
				IDispatcher sender,
				HttpContext httpContext,
				Guid connectionId,
				string key,
				[FromQuery] string? keyBase64 = null,
				[FromQuery] int? database = null) =>
				HandleGet(sender, httpContext, connectionId, key, keyBase64, database))
				.RequireAuthorization(Policies.Authenticated).WithOpenApi();

			routerBuilder.MapGet("storage/{connectionId:guid}/keys/{*key}", (
				IDispatcher sender,
				HttpContext httpContext,
				Guid connectionId,
				string key,
				[FromQuery] string? keyBase64 = null,
				[FromQuery] int? database = null) =>
				HandleGet(sender, httpContext, connectionId, key, keyBase64, database))
				.RequireAuthorization(Policies.Authenticated).WithOpenApi();
		}
	}

}
