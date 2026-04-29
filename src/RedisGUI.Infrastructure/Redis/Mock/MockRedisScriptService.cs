using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.Mock;
using RedisGUI.Shared.Primitives;
using System.Text.RegularExpressions;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Mock implementation of IRedisScriptService for testing
/// </summary>
public sealed partial class MockRedisScriptService : IRedisScriptService
{
	[GeneratedRegex(@"redis\.call\(['""]([^'""]+)['""],\s*['""]([^'""]+)['""]\)")]
	private static partial Regex RedisCallRegex();

	[GeneratedRegex(@"redis\.call\(['""]SET['""],\s*['""][^'""]+['""],\s*['""]([^'""]+)['""]\)")]
	private static partial Regex SetValueRegex();

	private readonly MockRedisStore _store;
	private readonly ILogger<MockRedisScriptService>? _logger;

	public MockRedisScriptService(MockRedisStore store, ILogger<MockRedisScriptService>? logger = null)
	{
		_store = store;
		_logger = logger;
	}

	public Task<Result<(string Result, string ScriptHash)>> ExecuteLuaScriptAsync(
		RedisConnection connection,
		string script,
		string[] keys,
		string[] arguments,
		CancellationToken cancellationToken = default)
	{
		try
		{
			// Generate SHA1 hash for the script
			string scriptHash = ComputeSha1(script);

			// Basic Lua script execution - parse and execute simple commands
			string result = ExecuteLuaScriptContent(script, keys, arguments);

			_logger?.LogDebug("Executed Lua script with hash {Hash}", scriptHash);
			return Task.FromResult(Result.Success((result, scriptHash)));
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error executing Lua script");
			return Task.FromResult(Result.Failure<(string, string)>(DomainErrors.Redis.ScriptFailed));
		}
	}

	private string ExecuteLuaScriptContent(string script, string[] keys, string[] arguments)
	{
		// Basic Lua script interpreter for common patterns
		// This is a simplified implementation that handles basic Redis commands in Lua

		// Replace KEYS[] and ARGV[] with actual values
		string processedScript = script;
		for (int i = 0; i < keys.Length; i++)
		{
			processedScript = processedScript.Replace($"KEYS[{i + 1}]", keys[i]);
		}
		for (int i = 0; i < arguments.Length; i++)
		{
			processedScript = processedScript.Replace($"ARGV[{i + 1}]", arguments[i]);
		}

		// Simple pattern matching for common Redis commands
		if (processedScript.Contains("redis.call"))
		{
			// Extract redis.call commands
			MatchCollection matches = RedisCallRegex().Matches(processedScript);
			foreach (Match match in matches)
			{
				string command = match.Groups[1].Value;
				string key = match.Groups[2].Value;

				if (command == "GET")
				{
					if (_store.TryGetValue(key, out var value) && value != null && !_store.IsExpired(value))
					{
						processedScript = processedScript.Replace(match.Value, $"'{value.StringValue}'");
					}
					else
					{
						processedScript = processedScript.Replace(match.Value, "nil");
					}
				}
				else if (command == "SET")
				{
					// Extract value from script
					Match valueMatch = SetValueRegex().Match(processedScript);
					if (valueMatch.Success)
					{
						_store[key] = new MockRedisValue
						{
							Type = "String",
							StringValue = valueMatch.Groups[1].Value,
							CreatedAt = DateTime.UtcNow
						};
						processedScript = processedScript.Replace(match.Value, "'OK'");
					}
				}
			}
		}

		// For now, return a mock result
		// In a production mock, you'd want a more sophisticated Lua interpreter
		return "mock-lua-result";
	}

	private static string ComputeSha1(string input)
	{
		byte[] hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(input));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}
}
