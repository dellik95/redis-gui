using System.Collections.Generic;

namespace RedisGUI.Domain.Redis;

/// <summary>
/// Configuration options for generating synthetic demo/seed data across Redis databases
/// </summary>
public sealed class SeedSampleDataOptions
{
	public int TotalKeys { get; set; } = 50;
	public int KeyCount { get => TotalKeys; set => TotalKeys = value; }

	public List<int>? Databases { get; set; }
	public List<int>? TargetDatabases { get => Databases; set => Databases = value; }

	public bool RandomizeDatabases { get; set; } = false;
	public bool DistributeRandomlyAcrossDatabases { get => RandomizeDatabases; set => RandomizeDatabases = value; }

	public int RandomDatabasesCount { get; set; } = 3;
	public int RandomDbCount { get => RandomDatabasesCount; set => RandomDatabasesCount = value; }

	public List<string>? DataTypes { get; set; }
	public string KeyComplexity { get; set; } = "hierarchical"; // simple, hierarchical, uuid, hashtag
	public string PayloadComplexity { get; set; } = "json"; // simple, json, complex
	public string TtlStrategy { get; set; } = "mixed"; // mixed, persistent, expiring

	public string? CustomPrefix { get; set; } = "demo";
	public string? KeyPrefix { get => CustomPrefix; set => CustomPrefix = value; }
}

/// <summary>
/// Execution summary of seeded sample data
/// </summary>
public sealed class SeedDataResult
{
	public int TotalKeysSeeded { get; set; }
	public int TotalKeysCreated { get => TotalKeysSeeded; set => TotalKeysSeeded = value; }

	public Dictionary<string, int> CountByType { get; set; } = new();
	public Dictionary<string, int> KeyCountPerType { get => CountByType; set => CountByType = value; }

	public List<int> DatabasesAffected { get; set; } = new();
	public List<int> TargetDatabases { get => DatabasesAffected; set => DatabasesAffected = value; }

	public string Message { get; set; } = string.Empty;
	public string Summary { get => Message; set => Message = value; }
}
