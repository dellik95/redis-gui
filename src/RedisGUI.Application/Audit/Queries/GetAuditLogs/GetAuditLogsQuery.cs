using FluentResults;
using System;
using DispatchWeave;
using DispatchWeave.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Audit;
using RedisGUI.Shared.Primitives;

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

/// <summary>
/// Handler for GetAuditLogsQuery
/// </summary>
internal sealed class GetAuditLogsQueryHandler : IQueryHandler<GetAuditLogsQuery, GetAuditLogsResponse>
{
	private readonly IAuditLogRepository auditLogRepository;

	public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
	{
		this.auditLogRepository = auditLogRepository;
	}

	public async Task<Result<GetAuditLogsResponse>> Handle(
		GetAuditLogsQuery request,
		CancellationToken cancellationToken)
	{
		int skip = Math.Max(0, request.Skip);
		int take = Math.Clamp(request.Take, 1, 1000);
		var (logs, totalCount) = await auditLogRepository.SearchAsync(
			request.UserId, request.EntityType, request.EntityId, request.FromDate, request.ToDate,
			skip, take, cancellationToken);

		var items = logs.Select(log => new AuditLogItem(
			log.Id,
			log.UserId,
			log.Action.ToString(),
			log.EntityType,
			log.EntityId,
			log.Timestamp,
			log.Metadata?.IpAddress,
			log.Metadata?.UserAgent,
			log.Metadata?.OldValue,
			log.Metadata?.NewValue
		)).ToList();

		return new GetAuditLogsResponse(items, totalCount, skip, take);
	}
}
