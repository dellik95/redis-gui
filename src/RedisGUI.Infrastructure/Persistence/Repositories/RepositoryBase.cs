using RedisGUI.Domain.Errors;
using System.Linq;
using System.Linq.Expressions;

namespace RedisGUI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Represents base repository 
/// </summary>
public class RepositoryBase<T> : IRepository<T> where T : Entity
{
	/// <summary>
	/// Represent set of data
	/// </summary>
	protected DbSet<T> DbSet;


	/// <summary>
	/// Creates a new instance of <see cref="RepositoryBase{T}"/>
	/// </summary>
	/// <param name="applicationDbContext">The database context to be used</param>
	public RepositoryBase(ApplicationDbContext applicationDbContext)
	{
		this.DbSet = applicationDbContext.Set<T>();
	}

	/// <summary>
	/// Adds a new Redis connection to the repository
	/// </summary>
	/// <param name="entity">The Redis connection entity to add</param>
	public void Add(T entity)
	{
		DbSet.Add(entity);
	}

	/// <summary>
	/// Deletes a Redis connection by its identifier
	/// </summary>
	/// <param name="id">The unique identifier of the connection</param>
	/// <param name="token">Cancellation token</param>
	/// <returns>A Result indicating the success or failure of the operation</returns>
	public async Task<Result> DeleteAsync(Guid id, CancellationToken token = default)
	{
		T? connection = await DbSet.FindAsync(id, token);

		if (connection is null)
		{
			return Result.Failure(DomainErrors.Storage.ItemNotFound);
		}

		DbSet.Remove(connection);

		return Result.Success();
	}

	/// <summary>
	/// Updates an existing Redis connection in the repository
	/// </summary>
	/// <param name="entity">The Redis connection entity to update</param>
	public void Update(T entity)
	{
		DbSet.Update(entity);
	}

	/// <inheritdoc />
	public async Task<Result<PagedResult<T>>> GetAsync(
		Expression<Func<T, bool>>? filter = null,
		bool trackChanges = false,
		int pageNumber = 1,
		int pageSize = 100,
		CancellationToken cancellationToken = default)
	{
		IQueryable<T> query = DbSet.AsQueryable();

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
		List<T> items = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		PagedResult<T> pagedResult = new(items, totalCount, pageNumber, pageSize);
		return Result.Success(pagedResult);
	}

	/// <inheritdoc />
	public async Task<Result<T?>> FindAsync(Guid id, CancellationToken cancellationToken = default)
	{
		T? entity = await DbSet.FindAsync([id], cancellationToken);
		return entity is null
			? Result.Failure<T?>(DomainErrors.Storage.ItemNotFound)
			: Result.Success(entity);
	}
}
