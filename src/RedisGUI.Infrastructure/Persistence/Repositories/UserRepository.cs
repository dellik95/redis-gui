using RedisGUI.Domain.Users;
using System.Linq;
using System.Linq.Expressions;

namespace RedisGUI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User aggregate
/// </summary>
internal sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{

	/// <summary>
	/// Gets a user by their unique identifier (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		return await context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
	}

	/// <summary>
	/// Gets a user by their email address (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
	{
		return await context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
	}

	/// <summary>
	/// Gets all users (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await context.Users
			.AsNoTracking()
			.OrderBy(u => u.Email.Value)
			.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// Checks if an email address is already registered (read-only, uses AsNoTracking)
	/// </summary>
	public async Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
	{
		return await context.Users
			.AsNoTracking()
			.AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
	}

	/// <summary>
	/// Adds a new user to the repository
	/// </summary>
	public void Add(User user)
	{
		context.Users.Add(user);
	}

	/// <summary>
	/// Removes a user from the repository
	/// </summary>
	public void Remove(User user)
	{
		context.Users.Remove(user);
	}

	// IRepository<User> implementation
	public async Task<Result<PagedResult<User>>> GetAsync(
		Expression<Func<User, bool>>? filter = null,
		bool trackChanges = false,
		int pageNumber = 1,
		int pageSize = 100,
		CancellationToken cancellationToken = default)
	{
		IQueryable<User> query = context.Users.AsQueryable();

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
		List<User> items = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		PagedResult<User> pagedResult = new(items, totalCount, pageNumber, pageSize);
		return Result.Success(pagedResult);
	}

	public async Task<Result<User?>> FindAsync(Guid id, CancellationToken cancellationToken = default)
	{
		User? entity = await context.Users.FindAsync([id], cancellationToken);
		return entity is null
			? Result.Failure<User?>(Domain.Errors.DomainErrors.User.NotFound)
			: Result.Success(entity);
	}

	public void Update(User entity)
	{
		context.Users.Update(entity);
	}

	public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
	{
		User? user = await GetByIdAsync(id, cancellationToken);
		if (user is null)
			return Result.Failure(Domain.Errors.DomainErrors.User.NotFound);

		context.Users.Remove(user);
		return Result.Success();
	}
}
