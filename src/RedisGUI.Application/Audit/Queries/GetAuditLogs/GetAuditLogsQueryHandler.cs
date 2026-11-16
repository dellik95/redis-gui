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

