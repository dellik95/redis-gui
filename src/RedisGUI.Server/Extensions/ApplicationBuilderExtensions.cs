using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Persistence;
using System.Linq;
using Bogus;
using RedisGUI.Domain.Connection;

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
		var dbConfig = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>();

		if (dbConfig.Value.IsInMemory)
		{
			return;
		}

		using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		if (context.Database.GetPendingMigrations().Any())
		{
			context.Database.Migrate();
		}
	}


	/// <summary>
	/// Add dummy data to storage in testing purpose
	/// </summary>
	/// <param name="app">The application builder instance</param>
	public static void SeedFakeData(this IApplicationBuilder app)
	{
		using var scope = app.ApplicationServices.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var faker = new Faker();
		var connections = new List<RedisConnection>();

		foreach (var _ in Enumerable.Range(0, 100))
		{
			var connectionName = new ConnectionName(faker.Address.Country());
			var connectionHost = new ConnectionHost(faker.Internet.Url());
			var connectionPort = new ConnectionPort(faker.Internet.Port());
			var dbNumber = faker.Random.Number(1, 10);
			connections.Add(RedisConnection.Create(connectionName, connectionHost, connectionPort, dbNumber));
		}

		context.Connections.AddRange(connections);
		context.SaveChanges();
	}
}
