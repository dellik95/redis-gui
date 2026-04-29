using RedisGUI.Domain.Connection;

namespace RedisGUI.Domain.Redis.Services;

/// <summary>
/// Domain service for importing data into Redis
/// </summary>
public interface IRedisDataImporter
{
	/// <summary>
	/// Imports data into the Redis database
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="data">The data to import</param>
	/// <param name="format">The format of the data (JSON, CSV)</param>
	/// <param name="overwriteExisting">Whether to overwrite existing keys</param>
	/// <returns>Result containing import statistics</returns>
	Task<Result<ImportResult>> ImportDataAsync(
		RedisConnection connection,
		string data,
		ImportFormat format,
		bool overwriteExisting);
}

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResult
{
	/// <summary>
	/// Gets or sets the number of keys imported
	/// </summary>
	public int ImportedCount { get; set; }

	/// <summary>
	/// Gets or sets the number of keys skipped
	/// </summary>
	public int SkippedCount { get; set; }

	/// <summary>
	/// Gets or sets the number of keys that failed to import
	/// </summary>
	public int FailedCount { get; set; }

	/// <summary>
	/// Gets or sets the collection of failed keys with error messages
	/// </summary>
	public List<FailedImport> FailedKeys { get; set; } = [];
}

/// <summary>
/// Details of a failed import
/// </summary>
public class FailedImport
{
	/// <summary>
	/// Gets or sets the key that failed to import
	/// </summary>
	public required string Key { get; set; }

	/// <summary>
	/// Gets or sets the error message
	/// </summary>
	public required string ErrorMessage { get; set; }
}
