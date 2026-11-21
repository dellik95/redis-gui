using DispatchWeave;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Audit.Queries.GetAuditLogs;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.Audit;

/// <summary>
/// Endpoint for retrieving audit logs
/// </summary>
public class GetAuditLogsEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for getting audit logs
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("audit", async (
			IDispatcher sender,
			Guid? userId,
			string? entityType,
			Guid? entityId,
			DateTime? fromDate,
			DateTime? toDate,
			int skip = 0,
			int take = 50,
			CancellationToken cancellationToken = default) =>
		{
			GetAuditLogsQuery query = new(
				userId,
				entityType,
				entityId,
				fromDate,
				toDate,
				skip,
				take);

			return await sender.Send(query, cancellationToken)
				.Match(Results.Ok, error => error.ToProblemDetails());
		})
		.WithName("GetAuditLogs")
		.WithSummary("Get audit logs")
		.WithDescription("Retrieves audit logs with optional filtering by user, entity, or date range. Admin only.")
		.WithTags("Audit")
		.Produces<GetAuditLogsResponse>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest)
		.RequireAuthorization(Policies.AdminOnly);
	}
}

