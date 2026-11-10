using System.Text.RegularExpressions;

namespace RedisGUI.Infrastructure.Redis.Common;

/// <summary>
/// Provides unified Redis glob-style pattern matching (*, ?, [a-z]).
/// </summary>
public static class RedisPatternMatcher
{
	/// <summary>
	/// Determines if the given key matches the Redis pattern.
	/// </summary>
	/// <param name="key">The key string to test.</param>
	/// <param name="pattern">Redis pattern (e.g. "*", "user:*", "item:?").</param>
	/// <returns>True if key matches pattern; otherwise false.</returns>
	public static bool IsMatch(string key, string pattern)
	{
		if (string.IsNullOrEmpty(pattern) || pattern == "*")
		{
			return true;
		}

		if (string.IsNullOrEmpty(key))
		{
			return false;
		}

		// Fast path for prefix matching (e.g. "prefix:*")
		if (pattern.EndsWith('*') && !pattern[..^1].Contains('*') && !pattern[..^1].Contains('?'))
		{
			return key.StartsWith(pattern[..^1], StringComparison.Ordinal);
		}

		// Fast path for suffix matching (e.g. "*:suffix")
		if (pattern.StartsWith('*') && !pattern[1..].Contains('*') && !pattern[1..].Contains('?'))
		{
			return key.EndsWith(pattern[1..], StringComparison.Ordinal);
		}

		// General regex matching for complex patterns
		string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
		return Regex.IsMatch(key, regexPattern, RegexOptions.IgnoreCase);
	}
}
