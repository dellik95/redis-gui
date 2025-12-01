using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetGroupedRedisKeys;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetGroupedRedisKeys;

/// <summary>
/// Endpoint for retrieving grouped Redis keys with tree/flat view options
/// </summary>
public class GetGroupedRedisKeysEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for getting grouped Redis keys
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("storage/{connectionId:guid}/grouped", async (
			IMediator sender,
			Guid connectionId,
			string? pattern,
			string? delimiter,
			string? viewMode) =>
		{
			var query = new GetGroupedRedisKeysQuery(
				connectionId,
				pattern ?? "*",
				delimiter ?? ":",
				viewMode ?? "tree");

			return await sender.Send<GetGroupedRedisKeysResponse>(query).Match(Results.Ok, Results.BadRequest);
		})
		.WithName("GetGroupedRedisKeys")
		.WithSummary("Get Redis keys grouped by delimiter")
		.WithDescription("Retrieves Redis keys grouped hierarchically by delimiter (e.g., ':' or '/') with tree or flat view mode");
	}
}
