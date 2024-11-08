using RedisGUI.Domain.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace RedisGUI.Domain.Extensions;

/// <summary>
/// Extension methods for working with Error objects
/// </summary>
public static class ErrorExtensions
{
	/// <summary>
	/// Converts a collection of errors to a comma-separated string of error codes
	/// </summary>
	/// <param name="errors">The collection of errors</param>
	/// <returns>A string containing all error codes separated by commas</returns>
	public static string ToCodeString(this IEnumerable<Error> errors)
	{
		errors ??= [];
		return string.Join(", ", errors.Select(x => x.Code));
	}

	/// <summary>
	/// Converts a collection of errors to a formatted multi-line string
	/// </summary>
	/// <param name="errors">The collection of errors</param>
	/// <returns>A string containing all error codes and messages, one per line</returns>
	public static string ToFormattedMessage(this IEnumerable<Error> errors)
	{
		errors ??= [];
		return string.Join('\n', errors.Select(x => $"{x.Code}: {x.Message}"));
	}
}
