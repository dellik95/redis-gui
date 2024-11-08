using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Persistence;
using System.Linq;

namespace RedisGUI.Server.Extensions;

/// <summary>
/// Provides extension methods for IApplicationBuilder
/// </summary>
public static class ApplicationBuilderExtensions
{
	/// <summary>
	/// Applies any pending database migrations to the database
	/// </summary>
	/// <param name="app">The application builder instance</param>
	/// <remarks>
	/// Only applies migrations if there are pending migrations and the database is not in-memory.
	/// This method should be called during application startup.
	/// </remarks>
	public static void ApplyMigrations(this IApplicationBuilder app)
	{
		using var scope = app.ApplicationServices.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var dbConfig = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>();

		if (dbConfig.Value.IsInMemory)
		{
			return;
		}

		if (context.Database.GetPendingMigrations().Any())
		{
			context.Database.Migrate();
		}
	}
}
