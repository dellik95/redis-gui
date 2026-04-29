using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis Lua script execution
/// </summary>
public interface IRedisScriptService
{
	/// <summary>
	/// Execute a Lua script in Redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="script">Lua script content</param>
	/// <param name="keys">Redis keys that the script will access</param>
	/// <param name="arguments">Arguments to pass to the script</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Script result with hash for caching</returns>
	Task<Result<(string Result, string ScriptHash)>> ExecuteLuaScriptAsync(
		RedisConnection connection, 
		string script, 
		string[] keys, 
		string[] arguments,
		CancellationToken cancellationToken = default);
}
