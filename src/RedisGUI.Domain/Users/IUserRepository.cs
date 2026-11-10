using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Users;

/// <summary>
/// Repository interface for User aggregate
/// </summary>
public interface IUserRepository : IRepository<User>
{
	/// <summary>
	/// Gets a user by their unique identifier
	/// </summary>
	/// <param name="id">The user's unique identifier</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The user if found; otherwise, null</returns>
	Task<User?> GetByIdAsync(System.Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a user by their email address
	/// </summary>
	/// <param name="email">The user's email address</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The user if found; otherwise, null</returns>
	Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all users
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of all users</returns>
	Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if an email address is already registered
	/// </summary>
	/// <param name="email">The email address to check</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if the email exists; otherwise, false</returns>
	Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new user to the repository
	/// </summary>
	/// <param name="user">The user to add</param>
	new void Add(User user);

	/// <summary>
	/// Removes a user from the repository
	/// </summary>
	/// <param name="user">The user to remove</param>
	void Remove(User user);
}
