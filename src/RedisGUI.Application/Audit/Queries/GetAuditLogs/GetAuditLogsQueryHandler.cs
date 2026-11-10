using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Audit;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Audit.Queries.GetAuditLogs;

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
		List<AuditLog> logs;
		int totalCount;

		if (request.UserId.HasValue)
		{
			logs = await auditLogRepository.GetByUserIdAsync(
				request.UserId.Value,
				request.Skip,
				request.Take,
				cancellationToken);
			totalCount = logs.Count; // Simplified - in production would have separate count query
		}
		else if (!string.IsNullOrEmpty(request.EntityType) && request.EntityId.HasValue)
		{
			logs = await auditLogRepository.GetByEntityAsync(
				request.EntityType,
				request.EntityId.Value,
				cancellationToken);
			totalCount = logs.Count;
		}
		else if (request.FromDate.HasValue && request.ToDate.HasValue)
		{
			logs = await auditLogRepository.GetByDateRangeAsync(
				request.FromDate.Value,
				request.ToDate.Value,
				request.Skip,
				request.Take,
				cancellationToken);
			totalCount = logs.Count;
		}
		else
		{
			// Default: get recent logs
			logs = await auditLogRepository.GetByDateRangeAsync(
				DateTime.UtcNow.AddDays(-30),
				DateTime.UtcNow,
				request.Skip,
				request.Take,
				cancellationToken);
			totalCount = logs.Count;
		}

		var items = logs.Select(log => new AuditLogItem(
			log.Id,
			log.UserId,
			log.Action.ToString(),
			log.EntityType,
			log.EntityId,
			log.Timestamp,
			log.Metadata.IpAddress,
			log.Metadata.UserAgent,
			log.Metadata.OldValue,
			log.Metadata.NewValue
		)).ToList();

		return new GetAuditLogsResponse(items, totalCount, request.Skip, request.Take);
	}
}

