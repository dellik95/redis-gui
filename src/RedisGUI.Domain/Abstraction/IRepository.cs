using RedisGUI.Domain.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Abstraction;

/// <summary>
/// Represent repository contract for specified entity type
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<in T> where T : Entity
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
}
