using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RedisGUI.Infrastructure.Persistence.Cache;

/// <summary>
/// Defines methods for processing database commands during interception
/// </summary>
public interface IDbCommandInterceptorProcessor
{
	/// <summary>
	/// Processes commands after they have been executed
	/// </summary>
	/// <typeparam name="T">The type of the result</typeparam>
	/// <param name="command">The database command that was executed</param>
	/// <param name="context">The database context</param>
	/// <param name="result">The result of the command execution</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <returns>The processed result</returns>
	ValueTask<T> ProcessExecutedCommands<T>(DbCommand command, DbContext context, T result, CancellationToken cancellationToken = default);

	/// <summary>
	/// Processes commands before they are executed
	/// </summary>
	/// <typeparam name="T">The type of the result</typeparam>
	/// <param name="command">The database command to be executed</param>
	/// <param name="context">The database context</param>
	/// <param name="result">The current result</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <returns>The processed result</returns>
	ValueTask<T> ProcessExecutingCommands<T>(DbCommand command, DbContext context, T result, CancellationToken cancellationToken = default);
}
