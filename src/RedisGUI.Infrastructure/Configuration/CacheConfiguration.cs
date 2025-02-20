using System;

namespace RedisGUI.Infrastructure.Configuration
{
	/// <summary>
	/// Configuration for caching.
	/// </summary>
	public class CacheConfiguration
	{
		/// <summary>
		/// Configuration key used to identify cache settings in configuration sources
		/// </summary>
		public const string Key = "Cache";

		/// <summary>
		/// Gets or sets the prefix for cache key.
		/// </summary>
		public string KeyPrefix { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the list of tables to cache.
		/// </summary>
		public string[] CachedTables { get; set; } = [];

		/// <summary>
		/// Gets or sets the list of tables to ignore for caching.
		/// </summary>
		public string[] IgnoredTables { get; set; } = [];

		/// <summary>
		/// Gets or sets the Time-To-Live (TTL) for items in cache.
		/// </summary>
		public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);
	}
}
