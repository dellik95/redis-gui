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
public class RedisConnectionRepository : RepositoryBase<RedisConnection>, IRedisConnectionRepository
{
	/// <summary>
	/// Creates a new instance of RedisConnectionRepository
	/// </summary>
	/// <param name="applicationDbContext">The database context to be used</param>
	public RedisConnectionRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
	{
	}

	/// <summary>
	/// Retrieves a Redis connection by its identifier
	/// </summary>
	/// <param name="id">The unique identifier of the connection</param>
	/// <param name="token">Cancellation token</param>
	/// <returns>A Result containing the found connection or an error if not found</returns>
	public async Task<Result<RedisConnection>> GetConnectionByIdAsync(Guid id, CancellationToken token = default)
	{
		var connection = await DbSet.FirstOrDefaultAsync(c => c.Id == id, token);
		return Result.Create(connection, DomainErrors.Connection.ConnectionNotFound);
	}
}
