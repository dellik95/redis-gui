using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.ImportRedisData;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.Redis;

/// <summary>
/// Endpoint for importing Redis data.
/// </summary>
public class ImportRedisDataEndpoint : IEndpoint
{
	/// <summary>
	/// Maps the import Redis data endpoint.
	/// </summary>
	/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("storage/{connectionId:guid}/import",
			async (
				IMediator sender,
				Guid connectionId,
				ImportRequest request,
				CancellationToken cancellationToken) =>
			{
				ImportFormat importFormat = request.Format?.ToLower() == "csv" ? ImportFormat.Csv : ImportFormat.Json;
				ImportRedisDataCommand command = new(
					connectionId,
					request.Data,
					importFormat,
					request.Overwrite);

				return await sender.Send(command, cancellationToken).Match(Results.Ok, Results.BadRequest);
			})
			.WithName("ImportRedisData")
			.WithSummary("Import Redis data")
			.WithDescription("Imports Redis data from JSON or CSV format. Use with caution as this can overwrite existing keys!")
			.WithTags("Redis Storage")
			.Produces(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization(Policies.WriteAccess)
			.RequireRateLimiting("sensitive");
	}

	/// <summary>
	/// Request model for importing data.
	/// </summary>
	/// <param name="Data">Data to import.</param>
	/// <param name="Format">Format of the data (json or csv).</param>
	/// <param name="Overwrite">Whether to overwrite existing keys.</param>
	public sealed record ImportRequest(
		string Data,
		string Format = "json",
		bool Overwrite = false);
}
