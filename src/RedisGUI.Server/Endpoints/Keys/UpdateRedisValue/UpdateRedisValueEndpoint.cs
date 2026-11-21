using DispatchWeave;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.UpdateRedisValue;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Server.Endpoints.RedisStorage.UpdateRedisValue;

/// <summary>
/// Endpoint for updating Redis values with type-specific logic
/// </summary>
public class UpdateRedisValueEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for updating Redis values
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		async Task<IResult> HandleUpdate(
			IDispatcher sender,
			HttpContext httpContext,
			Guid connectionId,
			string key,
			string? keyBase64,
			UpdateRedisValueRequest request,
			int? database)
		{
			TimeSpan? ttl = request.Ttl.HasValue && request.Ttl.Value > 0
				? TimeSpan.FromSeconds(request.Ttl.Value)
				: null;

			var command = new UpdateRedisValueCommand(
				connectionId,
				key,
				request.Type,
				request.Value,
				ttl,
				database ?? request.Database,
				request.KeyBase64 ?? keyBase64);

			return await sender.Send(command).Match(() => Results.Ok(), error => error.ToProblemDetails(httpContext));
		}

		routerBuilder.MapPut("storage/{connectionId:guid}/key", (
			IDispatcher sender,
			HttpContext httpContext,
			Guid connectionId,
			[FromBody] UpdateRedisValueRequest request,
			[FromQuery] string? key = null,
			[FromQuery] string? keyBase64 = null,
			[FromQuery] int? database = null) =>
			HandleUpdate(sender, httpContext, connectionId, !string.IsNullOrEmpty(request.Key) ? request.Key : key ?? string.Empty, keyBase64, request, database))
		.RequireAuthorization(Policies.WriteAccess)
		.WithName("UpdateRedisValueByQuery")
		.WithSummary("Update a Redis value by query/Base64 key identity");

		routerBuilder.MapPut("storage/{connectionId:guid}/keys/{key}", (
			IDispatcher sender,
			HttpContext httpContext,
			Guid connectionId,
			string key,
			UpdateRedisValueRequest request,
			[FromQuery] string? keyBase64 = null,
			[FromQuery] int? database = null) =>
			HandleUpdate(sender, httpContext, connectionId, key, keyBase64, request, database))
		.RequireAuthorization(Policies.WriteAccess)
		.WithName("UpdateRedisValue")
		.WithSummary("Update a Redis value")
		.WithDescription("Updates a Redis value based on its type (String, List, Set, Hash, SortedSet, Stream)");
	}
}
