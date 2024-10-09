using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace RedisGUI.Infrastructure.Configuration;

public sealed class ConfigureDbContextOptionsBuilder : IConfigureOptions<DbContextOptionsBuilder>
{
	private readonly DatabaseConfiguration _options;

	public ConfigureDbContextOptionsBuilder(IOptions<DatabaseConfiguration> options)
	{
		_options = options.Value;
	}

	public void Configure(DbContextOptionsBuilder options)
	{
		if (options.IsConfigured)
		{
			return;
		}

		if (this._options.IsInMemory)
		{
			options.UseInMemoryDatabase("ApplicationDbContext");
			return;
		}

		options.UseSqlServer(this._options.ConnectionString, builder =>
		{
			builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
		});
	}
}