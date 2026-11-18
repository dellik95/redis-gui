using System;
using System.Collections.Generic;
using NUnit.Framework;
using RedisGUI.Infrastructure.Redis.Seeding;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public sealed class KeyNamingStrategiesTests
{
	private static readonly string[] AllDataTypes = ["string", "hash", "list", "set", "zset", "stream"];

	[TestCase("hierarchical", 100)]
	[TestCase("hierarchical", 250)]
	[TestCase("hierarchical", 1000)]
	[TestCase("hierarchical", 5000)]
	[TestCase("hierarchical", 10000)]
	[TestCase("simple", 250)]
	[TestCase("uuid", 250)]
	[TestCase("hashtag", 250)]
	public void GenerateKeyName_ProducesExactUniqueKeyCountAcrossAllDataTypes(string complexity, int requestedCount)
	{
		var strategy = KeyNamingStrategyFactory.GetStrategy(complexity);
		var rng = new Random(42);
		var keys = new HashSet<string>(StringComparer.Ordinal);

		for (int i = 0; i < requestedCount; i++)
		{
			string type = AllDataTypes[i % AllDataTypes.Length];
			string key = strategy.GenerateKeyName("demo", type, i, rng);
			keys.Add(key);
		}

		Assert.That(keys.Count, Is.EqualTo(requestedCount));
	}
}
