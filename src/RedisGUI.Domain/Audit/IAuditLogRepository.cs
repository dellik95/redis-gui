using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Audit;

/// <summary>
/// Repository interface for AuditLog aggregate
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
	/// <summary>
	/// Gets audit logs for a specific user
	/// </summary>
	/// <param name="userId">The user ID</param>
	/// <param name="skip">Number of records to skip</param>
	/// <param name="take">Number of records to take</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of audit logs</returns>
	Task<List<AuditLog>> GetByUserIdAsync(
		Guid userId,
		int skip = 0,
		int take = 100,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets audit logs for a specific entity
	/// </summary>
	/// <param name="entityType">The entity type</param>
	/// <param name="entityId">The entity ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of audit logs</returns>
	Task<List<AuditLog>> GetByEntityAsync(
		string entityType,
		Guid entityId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets audit logs within a date range
	/// </summary>
	/// <param name="fromDate">Start date</param>
	/// <param name="toDate">End date</param>
	/// <param name="skip">Number of records to skip</param>
	/// <param name="take">Number of records to take</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of audit logs</returns>
	Task<List<AuditLog>> GetByDateRangeAsync(
		DateTime fromDate,
		DateTime toDate,
		int skip = 0,
		int take = 100,
		CancellationToken cancellationToken = default);
}
