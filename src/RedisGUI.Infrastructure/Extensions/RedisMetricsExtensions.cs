using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RedisGUI.Domain.RedisMetrics;
using RedisGUI.Domain.RedisMetrics.Attributes;

namespace RedisGUI.Infrastructure.Extensions;

public static class RedisMetricsExtensions
{
	private static readonly PropertyInfo[] cachedProperties = typeof(Domain.RedisMetrics.RedisMetrics)
		.GetProperties(BindingFlags.Public | BindingFlags.Instance);

	private static readonly Dictionary<PropertyInfo, RedisMetricAttribute> cachedAttributes = cachedProperties
		.ToDictionary(
			prop => prop,
			prop => prop.GetCustomAttribute<RedisMetricAttribute>()
		);

	public static Domain.RedisMetrics.RedisMetrics PopulateMetrics(this Domain.RedisMetrics.RedisMetrics metrics, Dictionary<string, IEnumerable<Tuple<string, string>>> infoSections)
	{
		foreach (var property in cachedProperties)
		{
			var attribute = cachedAttributes[property];

			if (attribute == null || !infoSections.TryGetValue(attribute.Section, out var sectionData))
			{
				continue;
			}

			var sectionValue = attribute.ExtractValue(sectionData);

			if (string.IsNullOrEmpty(sectionValue))
			{
				continue;
			}

			var convertedValue = Convert.ChangeType(sectionValue, property.PropertyType);

			property.SetValue(metrics, convertedValue);
		}

		return metrics;
	}


	public static void CalculateMetrics(this Domain.RedisMetrics.RedisMetrics currentMetrics, Domain.RedisMetrics.RedisMetrics previousMetrics, int interval)
	{
		foreach (var property in cachedProperties)
		{
			var attribute = cachedAttributes[property];

			if (attribute is not { IsCalculated: true })
			{
				continue;
			}

			var currentPropertyValue = property.GetValue(currentMetrics)?.ToString();
			var previousPropertyValue = property.GetValue(previousMetrics)?.ToString();

			if (!double.TryParse(currentPropertyValue, out var currentValue) || !double.TryParse(previousPropertyValue, out var previousValue))
			{
				continue;
			}

			var rate = attribute.ProviderType switch
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

	private static string ExtractValue(this RedisMetricAttribute attribute, IEnumerable<Tuple<string, string>> sectionData)
	{
		if (!string.IsNullOrEmpty(attribute.Key))
		{
			return sectionData?.FirstOrDefault(x => x.Item1.Equals(attribute.Key, StringComparison.OrdinalIgnoreCase))?.Item2;
		}

		if (string.IsNullOrEmpty(attribute.ValuePattern))
		{
			return string.Empty;
		}

		var match = sectionData.Select(x => Regex.Match(x.Item2, attribute.ValuePattern)).FirstOrDefault(x => x.Success);

		if (match == null || match.Groups.Count == 0)
		{
			return string.Empty;
		}

		return match.Groups[1].Value;
	}

	private static double CalculateCpuUsage(Domain.RedisMetrics.RedisMetrics currentMetrics, Domain.RedisMetrics.RedisMetrics previousMetrics)
	{
		if (currentMetrics.UptimeInSeconds <= previousMetrics.UptimeInSeconds)
		{
			return 0.0;
		}

		var currentCpuUsage = currentMetrics.CpuUsageSystem + currentMetrics.CpuUsageUser;

		var previousCpuUsage = previousMetrics.CpuUsageSystem + previousMetrics.CpuUsageUser;

		var timeDelta = currentMetrics.UptimeInSeconds - previousMetrics.UptimeInSeconds;

		var usage = ((currentCpuUsage - previousCpuUsage) / timeDelta) * 100;

		if (usage < 0)
		{
			return 0.0;
		}

		return usage > 100.0 ? 100.0 : usage;
	}

}
