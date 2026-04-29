using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Audit.Queries.GetAuditLogs;

/// <summary>
/// Query to retrieve audit logs with filtering and pagination
/// </summary>
/// <param name="UserId">Optional user ID filter</param>
/// <param name="EntityType">Optional entity type filter</param>
/// <param name="EntityId">Optional entity ID filter</param>
/// <param name="FromDate">Optional start date filter</param>
/// <param name="ToDate">Optional end date filter</param>
/// <param name="Skip">Number of records to skip</param>
/// <param name="Take">Number of records to take</param>
public sealed record GetAuditLogsQuery(
	Guid? UserId = null,
	string? EntityType = null,
	Guid? EntityId = null,
	DateTime? FromDate = null,
	DateTime? ToDate = null,
	int Skip = 0,
	int Take = 50) : IQuery<GetAuditLogsResponse>;

