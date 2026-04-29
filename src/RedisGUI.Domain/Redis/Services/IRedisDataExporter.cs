using RedisGUI.Domain.Connection;

namespace RedisGUI.Domain.Redis.Services;

/// <summary>
/// Domain service for exporting Redis data
/// </summary>
public interface IRedisDataExporter
{
	/// <summary>
	/// Exports all data from the Redis database
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="format">The export format (JSON, CSV)</param>
	/// <returns>Result containing the exported data as a string</returns>
	Task<Result<string>> ExportDataAsync(RedisConnection connection, ExportFormat format);

	/// <summary>
	/// Exports data matching a pattern
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="pattern">The key pattern to match</param>
	/// <param name="format">The export format</param>
	/// <returns>Result containing the exported data</returns>
	Task<Result<string>> ExportDataByPatternAsync(
		RedisConnection connection,
		string pattern,
		ExportFormat format);
}
