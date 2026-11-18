using RedisGUI.Domain.RedisMetrics;
using RedisGUI.Domain.RedisMetrics.Attributes;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RedisGUI.Infrastructure.Extensions;

public static class RedisMetricsExtensions
{
	private static readonly PropertyInfo[] cachedProperties = typeof(Domain.RedisMetrics.RedisMetrics)
		.GetProperties(BindingFlags.Public | BindingFlags.Instance);

	private static readonly Dictionary<PropertyInfo, RedisMetricAttribute?> cachedAttributes = cachedProperties
		.ToDictionary(
			prop => prop,
			prop => prop.GetCustomAttribute<RedisMetricAttribute>()
		);

	public static Domain.RedisMetrics.RedisMetrics PopulateMetrics(this Domain.RedisMetrics.RedisMetrics metrics, Dictionary<string, IEnumerable<Tuple<string, string>>> infoSections)
	{
		foreach (PropertyInfo property in cachedProperties)
		{
			RedisMetricAttribute? attribute = cachedAttributes[property];

			if (attribute == null || !infoSections.TryGetValue(attribute.Section, out IEnumerable<Tuple<string, string>>? sectionData))
			{
				continue;
			}

			string? sectionValue = attribute.ExtractValue(sectionData);

			if (string.IsNullOrEmpty(sectionValue))
			{
				continue;
			}

			Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
			object convertedValue = Convert.ChangeType(sectionValue, targetType, CultureInfo.InvariantCulture);

			property.SetValue(metrics, convertedValue);
		}

		var dbCounts = new Dictionary<int, long>();
		for (int i = 0; i < 16; i++)
		{
			dbCounts[i] = 0;
		}

		if (infoSections.TryGetValue("Keyspace", out IEnumerable<Tuple<string, string>>? keyspaceData) && keyspaceData is not null)
		{
			foreach (var entry in keyspaceData)
			{
				if (entry.Item1.StartsWith("db", StringComparison.OrdinalIgnoreCase) &&
					int.TryParse(entry.Item1.AsSpan(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int dbIdx))
				{
					Match keysMatch = Regex.Match(entry.Item2, @"keys=(\d+)");
					if (keysMatch.Success &&
						long.TryParse(keysMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long count))
					{
						dbCounts[dbIdx] = count;
					}
				}
			}
		}

		metrics.DatabaseKeyCounts = dbCounts;
		return metrics;
	}


	public static void CalculateMetrics(this Domain.RedisMetrics.RedisMetrics currentMetrics, Domain.RedisMetrics.RedisMetrics previousMetrics, int interval)
	{
		foreach (PropertyInfo property in cachedProperties)
		{
			RedisMetricAttribute? attribute = cachedAttributes[property];

			if (attribute is not { IsCalculated: true })
			{
				continue;
			}

			string? currentPropertyValue = property.GetValue(currentMetrics)?.ToString();
			string? previousPropertyValue = property.GetValue(previousMetrics)?.ToString();

			if (!double.TryParse(currentPropertyValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentValue) ||
				!double.TryParse(previousPropertyValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double previousValue))
			{
				continue;
			}

			double rate = attribute.ProviderType switch
			{
				ValueProviderType.Delta => ((currentValue - previousValue) / 1),
				ValueProviderType.CpuUsage => CalculateCpuUsage(currentMetrics, previousMetrics),
				ValueProviderType.Percent => ((currentValue - previousValue) / interval) * 100,
				ValueProviderType.Raw => currentValue,
				_ => currentValue
			};

			property.SetValue(currentMetrics, rate);
		}
	}

	private static string? ExtractValue(this RedisMetricAttribute attribute, IEnumerable<Tuple<string, string>> sectionData)
	{
		if (!string.IsNullOrEmpty(attribute.Key))
		{
			return sectionData?.FirstOrDefault(x => x.Item1.Equals(attribute.Key, StringComparison.OrdinalIgnoreCase))?.Item2;
		}

		if (string.IsNullOrEmpty(attribute.ValuePattern))
		{
			return string.Empty;
		}

		long sum = 0;
		bool matchedAny = false;
		foreach (var item in sectionData)
		{
			Match match = Regex.Match(item.Item2, attribute.ValuePattern);
			if (match.Success && match.Groups.Count > 1)
			{
				if (long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long val))
				{
					sum += val;
					matchedAny = true;
				}
				else
				{
					return match.Groups[1].Value;
				}
			}
		}

		return matchedAny ? sum.ToString(CultureInfo.InvariantCulture) : string.Empty;
	}

	private static double CalculateCpuUsage(Domain.RedisMetrics.RedisMetrics currentMetrics, Domain.RedisMetrics.RedisMetrics previousMetrics)
	{
		if (currentMetrics.UptimeInSeconds <= previousMetrics.UptimeInSeconds)
		{
			return 0.0;
		}

		double currentCpuUsage = currentMetrics.CpuUsageSystem + currentMetrics.CpuUsageUser;

		double previousCpuUsage = previousMetrics.CpuUsageSystem + previousMetrics.CpuUsageUser;

		double timeDelta = currentMetrics.UptimeInSeconds - previousMetrics.UptimeInSeconds;

		double usage = ((currentCpuUsage - previousCpuUsage) / timeDelta) * 100;

		if (usage < 0)
		{
			return 0.0;
		}

		return usage > 100.0 ? 100.0 : usage;
	}

}
