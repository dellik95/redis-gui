using FluentAssertions;
using NUnit.Framework;
using RedisGUI.Infrastructure.Redis.Common;
using RedisGUI.Infrastructure.Redis.LoadTesting;
using RedisGUI.Infrastructure.Redis.Parsers;
using RedisGUI.Infrastructure.Redis.Seeding;
using System;
using System.Collections.Generic;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class RedisCommonTests
{
	[Test]
	public void PatternMatcher_PrefixPattern_MatchesCorrectly()
	{
		RedisPatternMatcher.IsMatch("user:100", "user:*").Should().BeTrue();
		RedisPatternMatcher.IsMatch("order:100", "user:*").Should().BeFalse();
		RedisPatternMatcher.IsMatch("anything", "*").Should().BeTrue();
		RedisPatternMatcher.IsMatch("file.json", "*.json").Should().BeTrue();
		RedisPatternMatcher.IsMatch("file.xml", "*.json").Should().BeFalse();
	}

	[Test]
	public void CommandLineTokenizer_WithQuotes_ParsesTokensCorrectly()
	{
		string command = "HSET \"my key\" field \"complex value with spaces\"";
		string[] tokens = CommandLineTokenizer.Tokenize(command);

		tokens.Should().HaveCount(4);
		tokens[0].Should().Be("HSET");
		tokens[1].Should().Be("my key");
		tokens[2].Should().Be("field");
		tokens[3].Should().Be("complex value with spaces");
	}

	[Test]
	public void RedisInfoParser_KeyspaceInfo_ParsesAllDatabases()
	{
		string rawKeyspace = @"# Keyspace
db0:keys=1420,expires=50,avg_ttl=3600
db1:keys=85,expires=0,avg_ttl=0
db5:keys=12,expires=12,avg_ttl=120";

		Dictionary<int, long> parsed = RedisInfoParser.ParseKeyspace(rawKeyspace);

		parsed.Should().ContainKey(0);
		parsed[0].Should().Be(1420);
		parsed[1].Should().Be(85);
		parsed[5].Should().Be(12);
		parsed[2].Should().Be(0); // non-existent dbs default to 0
	}

	[Test]
	public void BenchmarkResultBuilder_CalculatesPercentilesAndThroughputCorrectly()
	{
		List<double> latencies = [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0];

		var result = new BenchmarkResultBuilder()
			.WithCounters(successful: 10, failed: 0)
			.WithDuration(100) // 100ms
			.WithLatencies(latencies)
			.WithCleanedKeys(10)
			.WithMetadata("mixed", 2)
			.Build();

		result.TotalOperations.Should().Be(10);
		result.SuccessfulOperations.Should().Be(10);
		result.FailedOperations.Should().Be(0);
		result.OpsPerSecond.Should().Be(100.0); // 10 ops in 0.1s = 100 ops/s
		result.MinLatencyMs.Should().Be(0.1);
		result.MaxLatencyMs.Should().Be(1.0);
		result.CleanedKeysCount.Should().Be(10);
		result.OperationType.Should().Be("mixed");
	}

	[Test]
	public void KeyNamingStrategyFactory_ReturnsAppropriateStrategies()
	{
		var rng = new Random(42);

		var simple = KeyNamingStrategyFactory.GetStrategy("simple");
		simple.GenerateKeyName("demo", "string", 0, rng).Should().Be("demo:string:1");

		var uuid = KeyNamingStrategyFactory.GetStrategy("uuid");
		uuid.GenerateKeyName("demo", "hash", 0, rng).Should().StartWith("demo:hash:");

		var hierarchical = KeyNamingStrategyFactory.GetStrategy("hierarchical");
		hierarchical.GenerateKeyName("demo", "string", 5, rng).Should().Contain("corp:finance");
	}
}
