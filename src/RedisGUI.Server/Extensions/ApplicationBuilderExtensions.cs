using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Persistence;

namespace RedisGUI.Server.Extensions;

public static class ApplicationBuilderExtensions
{
	public static void ApplyMigrations(this IApplicationBuilder app)
	{
		using var scope = app.ApplicationServices.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var dbConfig = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>();

		if (!dbConfig.Value.IsInMemory && context.Database.GetPendingMigrations().Any())
		{
			context.Database.Migrate();
		}
	}
}