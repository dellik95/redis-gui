using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace RedisGUI.Infrastructure.Configuration;

/// <summary>
/// Configures Entity Framework DbContext options
/// </summary>
public sealed class ConfigureDbContextOptionsBuilder : IConfigureOptions<DbContextOptionsBuilder>
{
	/// <summary>
	/// The database configuration options
	/// </summary>
	private readonly DatabaseConfiguration options;

	/// <summary>
	/// Initializes a new instance of ConfigureDbContextOptionsBuilder
	/// </summary>
	/// <param name="options">Database configuration options</param>
	public ConfigureDbContextOptionsBuilder(IOptions<DatabaseConfiguration> options)
	{
		this.options = options.Value;
	}

	/// <summary>
	/// Configures the DbContext options based on configuration settings
	/// </summary>
	/// <param name="options">The options builder to configure</param>
	public void Configure(DbContextOptionsBuilder options)
	{
		if (options.IsConfigured)
		{
			return;
		}

		if (this.options.IsInMemory)
		{
			options.UseInMemoryDatabase("ApplicationDbContext");
			return;
		}

		options.UseMySQL(this.options.ConnectionString, builder =>
		{
			builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
		});
	}
}
