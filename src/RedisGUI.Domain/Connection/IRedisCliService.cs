using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service responsible for executing raw Redis commands in CLI mode.
/// </summary>
public interface IRedisCliService
{
	/// <summary>
	/// Executes a raw Redis CLI command and formats the response.
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="rawCommand">Raw command string entered by the user (e.g. "INFO", "HGETALL user:1").</param>
	/// <param name="database">Target database index override, if any.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Formatted CLI string output.</returns>
	Task<Result<string>> ExecuteCliCommandAsync(
		RedisConnection connection,
		string rawCommand,
		int? database = null,
		CancellationToken cancellationToken = default);
}
