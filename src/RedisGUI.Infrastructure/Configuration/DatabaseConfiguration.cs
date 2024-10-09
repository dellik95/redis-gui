namespace RedisGUI.Infrastructure.Configuration;

public class DatabaseConfiguration
{
	public const string Key = "Database";

	public bool IsInMemory { get; set; }

	public string ConnectionString { get; set; }
}