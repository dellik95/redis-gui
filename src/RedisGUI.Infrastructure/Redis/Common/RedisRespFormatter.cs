using StackExchange.Redis;
using System.Text;

namespace RedisGUI.Infrastructure.Redis.Common;

/// <summary>
/// Formats StackExchange.Redis Result objects (RESP2/RESP3) into CLI human-readable strings.
/// </summary>
public static class RedisRespFormatter
{
	/// <summary>
	/// Recursively formats a RedisResult into CLI display string.
	/// </summary>
	/// <param name="result">The RedisResult from database execution.</param>
	/// <param name="indent">Current indentation level for nested arrays.</param>
	/// <returns>Formatted display string.</returns>
	public static string Format(RedisResult result, int indent = 0)
	{
		if (result.IsNull)
		{
			return "(nil)";
		}

		if (result.Resp2Type == ResultType.Array)
		{
			RedisResult[]? items = (RedisResult[]?)result;
			if (items is null || items.Length == 0)
			{
				return "(empty array)";
			}

			StringBuilder sb = new();
			for (int i = 0; i < items.Length; i++)
			{
				string prefix = new string(' ', indent * 2) + $"{i + 1}) ";
				string itemFormatted = Format(items[i], indent + 1);
				sb.AppendLine($"{prefix}{itemFormatted}");
			}
			return sb.ToString().TrimEnd();
		}

		if (result.Resp2Type == ResultType.Integer)
		{
			return $"(integer) {result}";
		}

		if (result.Resp2Type == ResultType.SimpleString)
		{
			return result.ToString() ?? "OK";
		}

		return $"\"{result}\"";
	}
}
