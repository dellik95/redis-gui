using RedisGUI.Domain.Errors;

namespace RedisGUI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Represents concrete repository for managing Redis connections
/// </summary>
public class RedisConnectionRepository(ApplicationDbContext applicationDbContext)
	: RepositoryBase<RedisConnection>(applicationDbContext), IRedisConnectionRepository
{

	/// <summary>
	/// Retrieves a Redis connection by its identifier
	/// </summary>
	/// <param name="id">The unique identifier of the connection</param>
	/// <param name="token">Cancellation token</param>
	/// <returns>A Result containing the found connection or an error if not found</returns>
	public async Task<Result<RedisConnection>> GetConnectionByIdAsync(Guid id, CancellationToken token = default)
	{
		// Use FindAsync for better performance (uses primary key lookup)
		RedisConnection? connection = await DbSet.FindAsync([id], token);
		return connection is null
			? Result.Failure<RedisConnection>(DomainErrors.Connection.ConnectionNotFound)
			: Result.Success(connection);
	}

	/// <summary>
	/// Retrieves all Redis connections (read-only, uses AsNoTracking)
	/// </summary>
	/// <param name="token">Cancellation token</param>
	/// <returns>A Result containing the list of connections</returns>
	public async Task<Result<List<RedisConnection>>> GetAllConnectionsAsync(CancellationToken token = default)
	{
		List<RedisConnection> connections = await DbSet
			.AsNoTracking()
			.ToListAsync(token);
		return Result.Success(connections);
	}
}
