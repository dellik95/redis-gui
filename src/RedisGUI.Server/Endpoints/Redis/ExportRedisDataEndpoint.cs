using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.ExportRedisData;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Text;
using System.Threading;

namespace RedisGUI.Server.Endpoints.Redis;

/// <summary>
/// Endpoint for exporting Redis data.
/// </summary>
public class ExportRedisDataEndpoint : IEndpoint
{
	/// <summary>
	/// Maps the export Redis data endpoint.
	/// </summary>
	/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("storage/{connectionId:guid}/export",
			async (
				IMediator sender,
				Guid connectionId,
				string? pattern,
				string? format,
				CancellationToken cancellationToken) =>
			{
				ExportFormat exportFormat = format?.ToLower() == "csv" ? ExportFormat.Csv : ExportFormat.Json;
				ExportRedisDataQuery query = new(connectionId, pattern ?? "*", exportFormat);

				return await sender.Send(query, cancellationToken).Match(
					success => Results.File(
						Encoding.UTF8.GetBytes(success.Data),
						success.ContentType,
						success.FileName),
					error => Results.BadRequest(error));
			})
			.WithName("ExportRedisData")
			.WithSummary("Export Redis data")
			.WithDescription("Exports Redis data matching the specified pattern to JSON or CSV format as a downloadable file")
			.WithTags("Redis Storage")
			.Produces(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization(Policies.WriteAccess)
			.RequireRateLimiting("sensitive");
	}
}
