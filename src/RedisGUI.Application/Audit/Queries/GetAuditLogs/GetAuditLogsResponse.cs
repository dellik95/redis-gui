using System;
using System.Collections.Generic;

namespace RedisGUI.Application.Audit.Queries.GetAuditLogs;

/// <summary>
/// Response containing audit log entries
/// </summary>
/// <param name="Items">List of audit log entries</param>
/// <param name="TotalCount">Total number of matching records</param>
/// <param name="Skip">Number of records skipped</param>
/// <param name="Take">Number of records taken</param>
public sealed record GetAuditLogsResponse(
	List<AuditLogItem> Items,
	int TotalCount,
	int Skip,
	int Take);

/// <summary>
/// Audit log item DTO
/// </summary>
/// <param name="Id">Audit log ID</param>
/// <param name="UserId">User who performed the action</param>
/// <param name="Action">The action performed</param>
/// <param name="EntityType">Type of entity affected</param>
/// <param name="EntityId">ID of entity affected</param>
/// <param name="Timestamp">When the action occurred</param>
/// <param name="IpAddress">Client IP address</param>
/// <param name="UserAgent">Client user agent</param>
/// <param name="OldValues">Previous values (for updates)</param>
/// <param name="NewValues">New values (for creates/updates)</param>
public sealed record AuditLogItem(
	Guid Id,
	Guid? UserId,
	string Action,
	string EntityType,
	Guid? EntityId,
	DateTime Timestamp,
	string? IpAddress,
	string? UserAgent,
	string? OldValues,
	string? NewValues);

