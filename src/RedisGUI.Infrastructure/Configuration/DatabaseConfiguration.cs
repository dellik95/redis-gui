namespace RedisGUI.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for database connections
/// </summary>
public class DatabaseConfiguration
{
	/// <summary>
	/// Configuration key used to identify database settings in configuration sources
	/// </summary>
	public const string Key = "Database";

	/// <summary>
	/// Gets or sets a value indicating whether to use an in-memory database
	/// </summary>
	public bool IsInMemory { get; set; }

	/// <summary>
	/// Gets or sets the database connection string
	/// </summary>
	public string ConnectionString { get; set; }
}
