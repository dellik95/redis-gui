using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
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
			HttpContext context, IAuthorizationService authorization, IRedisConnectionRepository repository,
			IRedisKeyService keys, CancellationToken cancellationToken) =>
		{
			var connection = await repository.GetConnectionByIdAsync(connectionId, cancellationToken);
			if (connection.IsFailure) return Results.NotFound(new { message = connection.Error.Message });
			if (!(await authorization.AuthorizeAsync(context.User, connection.Value, new ConnectionOwnershipRequirement())).Succeeded)
				return Results.Forbid();
			var page = await keys.ScanKeysAsync(connection.Value, pattern ?? "*", database ?? connection.Value.DatabaseNumber.Value,
				cursor, count ?? 100, cancellationToken);
			return page.IsSuccess ? Results.Ok(page.Value) : Results.BadRequest(new { message = page.Error.Message });
		}).WithName("ScanRedisKeys").WithTags("Redis Storage").RequireAuthorization(Policies.Authenticated);
	}
}
