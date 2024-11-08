using Microsoft.EntityFrameworkCore;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace RedisGUI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Represents concrete repository for managing Redis connections
/// </summary>
public class RedisConnectionRepository : IRedisConnectionRepository
{
	private readonly ApplicationDbContext applicationDbContext;

	/// <summary>
	/// Creates a new instance of RedisConnectionRepository
	/// </summary>
	/// <param name="applicationDbContext">The database context to be used</param>
	public RedisConnectionRepository(ApplicationDbContext applicationDbContext)
	{
		this.applicationDbContext = applicationDbContext;
	}

	/// <summary>
	/// Retrieves a Redis connection by its identifier
	/// </summary>
	/// <param name="id">The unique identifier of the connection</param>
	/// <param name="token">Cancellation token</param>
	/// <returns>A Result containing the found connection or an error if not found</returns>
	public async Task<Result<RedisConnection>> GetConnectionByIdAsync(Guid id, CancellationToken token = default)
	{
		var connection = await applicationDbContext.Connections.FirstOrDefaultAsync(c => c.Id == id, token);
		return Result.Create(connection, DomainErrors.Connection.ConnectionNotFound);
	}

	/// <summary>
	/// Adds a new Redis connection to the repository
	/// </summary>
	/// <param name="entity">The Redis connection entity to add</param>
	public void Add(RedisConnection entity)
	{
		applicationDbContext.Connections.Add(entity);
	}

	/// <summary>
	/// Deletes a Redis connection by its identifier
	/// </summary>
	/// <param name="id">The unique identifier of the connection</param>
	/// <param name="token">Cancellation token</param>
	/// <returns>A Result indicating the success or failure of the operation</returns>
	public async Task<Result> DeleteAsync(Guid id, CancellationToken token = default)
	{
		var connection = await applicationDbContext.Connections.FindAsync(id, token);

		if (connection == null)
		{
			return Result.Failure(DomainErrors.Connection.ConnectionNotFound);
		}

		applicationDbContext.Remove(connection);

		return Result.Success();
	}

	/// <summary>
	/// Updates an existing Redis connection in the repository
	/// </summary>
	/// <param name="entity">The Redis connection entity to update</param>
	public void Update(RedisConnection entity)
	{
		applicationDbContext.Connections.Update(entity);
	}
}
