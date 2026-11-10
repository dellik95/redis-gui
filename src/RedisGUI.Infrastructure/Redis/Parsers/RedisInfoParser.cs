using RedisGUI.Domain.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace RedisGUI.Infrastructure.Redis.Parsers;

/// <summary>
/// Parser and mapper for Redis INFO telemetry and keyspace outputs.
/// </summary>
public static partial class RedisInfoParser
{
	[GeneratedRegex(@"^db(?<db>\d+):keys=(?<keys>\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex KeyspaceRegex();

	/// <summary>
	/// Parses raw INFO keyspace text into a dictionary mapping database index to key count.
	/// </summary>
	/// <param name="rawKeyspaceInfo">Raw text returned from `INFO keyspace`.</param>
	/// <returns>Dictionary with key counts for databases 0 through 15.</returns>
	public static Dictionary<int, long> ParseKeyspace(string? rawKeyspaceInfo)
	{
		Dictionary<int, long> result = new();

		if (!string.IsNullOrWhiteSpace(rawKeyspaceInfo))
		{
			string[] lines = rawKeyspaceInfo.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				Match match = KeyspaceRegex().Match(line.Trim());
				if (match.Success)
				{
					if (int.TryParse(match.Groups["db"].Value, out int dbNum) &&
						long.TryParse(match.Groups["keys"].Value, out long keysCount))
					{
						result[dbNum] = keysCount;
					}
				}
			}
		}

		for (int i = 0; i <= 15; i++)
		{
			result.TryAdd(i, 0);
		}

		return result;
	}

	/// <summary>
	/// Parses grouped Redis INFO sections into a strongly-typed RedisServerInfoDto.
	/// </summary>
	/// <param name="infoGroups">Grouped key-value pairs returned from StackExchange.Redis Server.InfoAsync().</param>
	/// <returns>Mapped RedisServerInfoDto.</returns>
	public static RedisServerInfoDto ParseServerInfo(IGrouping<string, KeyValuePair<string, string>>[] infoGroups)
	{
		var allSections = new List<RedisServerInfoSection>();
		var flatProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var group in infoGroups)
		{
			var sectionDict = new Dictionary<string, string>();
			foreach (var kvp in group)
			{
				sectionDict[kvp.Key] = kvp.Value;
				flatProps[kvp.Key] = kvp.Value;
			}
			allSections.Add(new RedisServerInfoSection(group.Key, sectionDict));
		}

		string redisVersion = GetString(flatProps, "redis_version", "unknown");
		string redisMode = GetString(flatProps, "redis_mode", "standalone");
		string os = GetString(flatProps, "os", "unknown");
		long uptimeInSeconds = GetLong(flatProps, "uptime_in_seconds", 0);
		long connectedClients = GetLong(flatProps, "connected_clients", 0);
		long usedMemoryBytes = GetLong(flatProps, "used_memory", 0);
		long totalSystemMemoryBytes = GetLong(flatProps, "total_system_memory", 0);
		double memoryFragmentationRatio = GetDouble(flatProps, "mem_fragmentation_ratio", 1.0);
		long totalCommandsProcessed = GetLong(flatProps, "total_commands_processed", 0);
		long keyspaceHits = GetLong(flatProps, "keyspace_hits", 0);
		long keyspaceMisses = GetLong(flatProps, "keyspace_misses", 0);

		double hitRate = (keyspaceHits + keyspaceMisses) > 0
			? (keyspaceHits * 100.0 / (keyspaceHits + keyspaceMisses))
			: 0.0;

		return new RedisServerInfoDto(
			redisVersion,
			redisMode,
			os,
			uptimeInSeconds,
			connectedClients,
			usedMemoryBytes,
			totalSystemMemoryBytes,
			memoryFragmentationRatio,
			totalCommandsProcessed,
			keyspaceHits,
			keyspaceMisses,
			Math.Round(hitRate, 2),
			allSections);
	}

	private static string GetString(Dictionary<string, string> dict, string key, string fallback) =>
		dict.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

	private static long GetLong(Dictionary<string, string> dict, string key, long fallback) =>
		dict.TryGetValue(key, out string? value) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
			? parsed
			: fallback;

	private static double GetDouble(Dictionary<string, string> dict, string key, double fallback) =>
		dict.TryGetValue(key, out string? value) && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed)
			? parsed
			: fallback;
}
