using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;

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
		var connection = await DbSet.FindAsync(id, token);

		if (connection == null)
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
	public async Task<Result<List<T>>> GetAsync(Expression<Func<T, bool>> filter = null, bool trackChanges = false, int pageNumber = 1, int pageSize = 100)
	{
		//TODO : Add pagination
		var query = DbSet.AsQueryable();

		if (filter != null)
		{
			query = DbSet.Where(filter);
		}

		if (!trackChanges)
		{
			query = query.AsNoTracking();
		}

		var items = await query.ToListAsync();

		return items;
	}
}
