namespace RedisGUI.Application.Redis.Commands.ExecuteLuaScript;

/// <summary>
/// Response from executing a Lua script.
/// </summary>
/// <param name="Result">The result returned by the script.</param>
/// <param name="Success">Indicates if execution was successful.</param>
/// <param name="ScriptHash">SHA1 hash of the script (for caching).</param>
public sealed record ExecuteLuaScriptResponse(
	string Result,
	bool Success,
	string? ScriptHash = null);
