using RedisGUI.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Abstraction;

/// <summary>
/// Represent repository contract for specified entity type
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : Entity
{
	/// <summary>
	/// Add entity
	/// </summary>
	/// <param name="entity">Entity to add</param>
	void Add(T entity);

	/// <summary>
	/// Delete entity async
	/// </summary>
	/// <param name="id">Entity id</param>
	/// <param name="token">Cancellation token</param>
	/// <returns>A Result indicating success or failure of the deletion operation</returns>
	Task<Result> DeleteAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Update entity
	/// </summary>
	/// <param name="entity">Entity to update</param>
	void Update(T entity);

	/// <summary>
	/// Get entities filtered with specified filter.
	/// </summary>
	/// <param name="filter">Predicate to filter entities</param>
	/// <param name="trackChanges">Indicates whether to track entity changes or not</param>
	/// <param name="pageNumber">Page number</param>
	/// <param name="pageSize">Items per page</param>
	/// <returns></returns>
	Task<Result<List<T>>> GetAsync(Expression<Func<T, bool>> filter = null, bool trackChanges = false, int pageNumber = 1, int pageSize = 100);
}
