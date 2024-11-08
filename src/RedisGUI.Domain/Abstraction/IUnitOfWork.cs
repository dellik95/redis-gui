using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Abstraction;

/// <summary>
/// Represents a unit of work pattern interface for managing transactions and data persistence
/// </summary>
public interface IUnitOfWork
{
	/// <summary>
	/// Saves all changes made in this context to the database asynchronously
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation</param>
	/// <returns>The number of state entries written to the database</returns>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
