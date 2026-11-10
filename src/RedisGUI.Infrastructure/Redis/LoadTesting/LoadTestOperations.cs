using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.Redis.LoadTesting;

/// <summary>
/// Strategy interface for a synthetic load test operation.
/// </summary>
public interface ILoadTestOperation
{
	/// <summary>
	/// Executes a single synthetic benchmark operation against Redis.
	/// </summary>
	Task<bool> ExecuteAsync(IDatabase db, string prefix, int opIndex, string samplePayload, int warmKeysCount);
}

/// <summary>
/// Write-only synthetic load testing operation.
/// </summary>
public sealed class WriteLoadTestOperation : ILoadTestOperation
{
	public async Task<bool> ExecuteAsync(IDatabase db, string prefix, int opIndex, string samplePayload, int warmKeysCount)
	{
		string key = $"{prefix}k:{opIndex}";
		return await db.StringSetAsync(key, samplePayload);
	}
}

/// <summary>
/// Read-only synthetic load testing operation.
/// </summary>
public sealed class ReadLoadTestOperation : ILoadTestOperation
{
	public async Task<bool> ExecuteAsync(IDatabase db, string prefix, int opIndex, string samplePayload, int warmKeysCount)
	{
		string key = $"{prefix}warm:{opIndex % warmKeysCount}";
		var val = await db.StringGetAsync(key);
		return val.HasValue;
	}
}

/// <summary>
/// Mixed 50/50 read-write synthetic load testing operation.
/// </summary>
public sealed class MixedLoadTestOperation : ILoadTestOperation
{
	public async Task<bool> ExecuteAsync(IDatabase db, string prefix, int opIndex, string samplePayload, int warmKeysCount)
	{
		if (opIndex % 2 == 0)
		{
			string key = $"{prefix}k:{opIndex}";
			return await db.StringSetAsync(key, samplePayload);
		}
		else
		{
			string key = $"{prefix}warm:{opIndex % warmKeysCount}";
			var val = await db.StringGetAsync(key);
			return val.HasValue;
		}
	}
}

/// <summary>
/// Factory for resolving load test operation strategies.
/// </summary>
public static class LoadTestOperationFactory
{
	private static readonly ILoadTestOperation WriteOp = new WriteLoadTestOperation();
	private static readonly ILoadTestOperation ReadOp = new ReadLoadTestOperation();
	private static readonly ILoadTestOperation MixedOp = new MixedLoadTestOperation();

	/// <summary>
	/// Resolves operation strategy by operation type.
	/// </summary>
	public static ILoadTestOperation GetOperation(string? opType)
	{
		return (opType ?? "mixed").ToLowerInvariant() switch
		{
			"write" => WriteOp,
			"read" => ReadOp,
			_ => MixedOp
		};
	}
}
