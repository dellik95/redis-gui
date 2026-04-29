using RedisGUI.Domain.Users;
using System.Linq;

namespace RedisGUI.Infrastructure.Persistence;

/// <summary>
/// Compiled queries for frequently executed database operations to improve performance
/// </summary>
public static class CompiledQueries
{
	/// <summary>
	/// Compiled query to get a connection by ID
	/// </summary>
	public static readonly Func<ApplicationDbContext, Guid, Task<RedisConnection?>>
		GetConnectionById = EF.CompileAsyncQuery(
			(ApplicationDbContext context, Guid id) =>
				context.Set<RedisConnection>().FirstOrDefault(c => c.Id == id));

	/// <summary>
	/// Compiled query to get a user by email
	/// </summary>
	public static readonly Func<ApplicationDbContext, string, Task<User?>>
		GetUserByEmail = EF.CompileAsyncQuery(
			(ApplicationDbContext context, string email) =>
				context.Set<User>()
					.FirstOrDefault(u => u.Email.Value == email));

	/// <summary>
	/// Compiled query to check if a connection exists by ID
	/// </summary>
	public static readonly Func<ApplicationDbContext, Guid, Task<bool>>
		ConnectionExists = EF.CompileAsyncQuery(
			(ApplicationDbContext context, Guid id) =>
				context.Set<RedisConnection>().Any(c => c.Id == id));
}

