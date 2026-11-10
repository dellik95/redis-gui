using System.Collections.Generic;
using System.Text;

namespace RedisGUI.Infrastructure.Redis.Common;

/// <summary>
/// Tokenizes raw command lines preserving single and double quoted arguments.
/// </summary>
public static class CommandLineTokenizer
{
	/// <summary>
	/// Parses a command line string into an array of argument tokens.
	/// </summary>
	/// <param name="input">Raw command line entered by user.</param>
	/// <returns>Array of parsed arguments.</returns>
	public static string[] Tokenize(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return [];
		}

		List<string> parts = [];
		bool inQuotes = false;
		char quoteChar = '\0';
		StringBuilder current = new();

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if ((c == '"' || c == '\'') && (!inQuotes || c == quoteChar))
			{
				inQuotes = !inQuotes;
				quoteChar = inQuotes ? c : '\0';
			}
			else if (char.IsWhiteSpace(c) && !inQuotes)
			{
				if (current.Length > 0)
				{
					parts.Add(current.ToString());
					current.Clear();
				}
			}
			else
			{
				current.Append(c);
			}
		}

		if (current.Length > 0)
		{
			parts.Add(current.ToString());
		}

		return [.. parts];
	}
}
