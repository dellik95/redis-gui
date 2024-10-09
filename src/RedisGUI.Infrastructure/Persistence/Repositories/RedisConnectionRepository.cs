using Microsoft.EntityFrameworkCore;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Infrastructure.Persistence.Repositories;

public class RedisConnectionRepository : IRedisConnectionRepository
{
	private readonly ApplicationDbContext _applicationDbContext;

	public RedisConnectionRepository(ApplicationDbContext applicationDbContext)
	{
		_applicationDbContext = applicationDbContext;
	}

	public async Task<Result<RedisConnection>> GetConnectionByIdAsync(Guid id, CancellationToken token = default)
	{
		var connection = await this._applicationDbContext.Connections.FirstOrDefaultAsync(c => c.Id == id, token);
		return Result.Create(connection, DomainErrors.Connection.ConnectionNotFound);
	}

	public void Add(RedisConnection connection)
	{
		this._applicationDbContext.Connections.Add(connection);
	}

	public async Task<Result> DeleteAsync(Guid id, CancellationToken token = default)
	{
		var connection = await this._applicationDbContext.Connections.FindAsync(id);

		if (connection == null)
		{
			return Result.Failure(DomainErrors.Connection.ConnectionNotFound);
		}

		this._applicationDbContext.Remove(connection);

		return Result.Success();
	}

	public void Update(RedisConnection connection)
	{
		this._applicationDbContext.Connections.Update(connection);
	}
}