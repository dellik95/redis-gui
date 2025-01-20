using System;

namespace RedisGUI.Domain.RedisMetrics.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class RedisMetricAttribute : Attribute
{
	public string Section { get; }
	public string Key { get; }
	public string ValuePattern { get; }
	public ValueProviderType ProviderType { get; }
	public bool IsCalculated { get; }

	public RedisMetricAttribute(string section, string key = null, bool isCalculated = false, string valuePattern = null, ValueProviderType providerType = ValueProviderType.Raw)
	{
		if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(valuePattern))
		{
			throw new ArgumentNullException($"You should specify one of {nameof(key)} or {nameof(valuePattern)} argument");
		}

		Section = section;
		Key = key;
		ValuePattern = valuePattern;
		ProviderType = providerType;
		IsCalculated = isCalculated;
	}
}
