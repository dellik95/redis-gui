using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Errors;
using System.Linq;
using System.Linq.Expressions;

namespace RedisGUI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for AuditLog aggregate
/// </summary>
internal sealed class AuditLogRepository(ApplicationDbContext context) : IAuditLogRepository
{
	/// <summary>
	/// Gets an audit log by its unique identifier (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		return await context.AuditLogs
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
	}

	/// <summary>
	/// Gets all audit logs (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<List<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await context.AuditLogs
			.AsNoTracking()
			.OrderByDescending(a => a.Timestamp)
			.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Gets audit logs for a specific user (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<List<AuditLog>> GetByUserIdAsync(
		Guid userId,
		int skip = 0,
		int take = 100,
		CancellationToken cancellationToken = default)
	{
		return await context.AuditLogs
			.AsNoTracking()
			.Where(a => a.UserId == userId)
			.OrderByDescending(a => a.Timestamp)
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Gets audit logs for a specific entity (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<List<AuditLog>> GetByEntityAsync(
		string entityType,
		Guid entityId,
		CancellationToken cancellationToken = default)
	{
		return await context.AuditLogs
			.AsNoTracking()
			.Where(a => a.EntityType == entityType && a.EntityId == entityId)
			.OrderByDescending(a => a.Timestamp)
			.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Gets audit logs within a date range (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<List<AuditLog>> GetByDateRangeAsync(
		DateTime fromDate,
		DateTime toDate,
		int skip = 0,
		int take = 100,
		CancellationToken cancellationToken = default)
	{
		return await context.AuditLogs
			.AsNoTracking()
			.Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate)
			.OrderByDescending(a => a.Timestamp)
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Adds a new audit log
	/// </summary>
	public void Add(AuditLog auditLog)
	{
		context.AuditLogs.Add(auditLog);
	}

	/// <summary>
	/// Removes an audit log
	/// </summary>
	public void Remove(AuditLog auditLog)
	{
		context.AuditLogs.Remove(auditLog);
	}

	// IRepository<AuditLog> implementation
	public async Task<Result<PagedResult<AuditLog>>> GetAsync(
		Expression<Func<AuditLog, bool>>? filter = null,
		bool trackChanges = false,
		int pageNumber = 1,
		int pageSize = 100,
		CancellationToken cancellationToken = default)
	{
		IQueryable<AuditLog> query = context.AuditLogs.AsQueryable();

		if (filter != null)
		{
			query = query.Where(filter);
		}

		if (!trackChanges)
		{
			query = query.AsNoTracking();
		}

		// Validate pagination parameters
		if (pageNumber < 1)
			pageNumber = 1;
		if (pageSize < 1)
			pageSize = 100;
		if (pageSize > 1000)
			pageSize = 1000; // Cap at 1000 for safety

		int totalCount = await query.CountAsync(cancellationToken);
		List<AuditLog> items = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		PagedResult<AuditLog> pagedResult = new(items, totalCount, pageNumber, pageSize);
		return Result.Success(pagedResult);
	}

	public async Task<Result<AuditLog?>> FindAsync(Guid id, CancellationToken cancellationToken = default)
	{
		AuditLog? entity = await context.AuditLogs.FindAsync([id], cancellationToken);
		return entity is null
			? Result.Failure<AuditLog?>(DomainErrors.Storage.ItemNotFound)
			: Result.Success(entity);
	}

	public void Update(AuditLog entity)
	{
		context.AuditLogs.Update(entity);
	}

	public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
	{
		AuditLog? auditLog = await GetByIdAsync(id, cancellationToken);
		if (auditLog is null)
			return Result.Failure(DomainErrors.AuditLog.NotFound);

		context.AuditLogs.Remove(auditLog);
		return Result.Success();
	}
}
