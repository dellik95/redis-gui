using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Domain.Connection;
using RedisGUI.Application.Redis.Queries.ScanRedisKeys;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetAllRedisKeys;

/// <summary>Incrementally scans key metadata without loading values.</summary>
public sealed class ScanRedisKeysEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("connections/{connectionId:guid}/key-scan", async (
			Guid connectionId, string? pattern, int? database, string? cursor, int? count,
			IMediator mediator, CancellationToken cancellationToken) =>
		{
			var result = await mediator.Send<RedisKeyScanPage>(new ScanRedisKeysQuery(connectionId, pattern ?? "*", database, cursor, count ?? 100), cancellationToken);
			return result.ToHttpResult();
		}).WithName("ScanRedisKeys").WithTags("Redis Storage").RequireAuthorization(Policies.Authenticated);
	}
}
