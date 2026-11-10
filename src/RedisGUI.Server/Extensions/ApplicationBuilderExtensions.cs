using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Bogus;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Users;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Persistence;

namespace RedisGUI.Server.Extensions;

/// <summary>
/// Provides extension methods for IApplicationBuilder to configure application startup behavior.
/// </summary>
public static class ApplicationBuilderExtensions
{
	/// <summary>
	/// Ensures database schema is up-to-date and seeds default developer/admin credentials and connection.
	/// </summary>
	public static void EnsureDatabaseReady(this IApplicationBuilder app)
	{
		var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

		using (var migrationScope = app.ApplicationServices.CreateScope())
		{
			var services = migrationScope.ServiceProvider;
			var dbConfig = services.GetRequiredService<IOptions<DatabaseConfiguration>>();

			if (!dbConfig.Value.IsInMemory)
			{
				var context = services.GetRequiredService<ApplicationDbContext>();
				const int maxRetries = 10;
				for (int attempt = 1; attempt <= maxRetries; attempt++)
				{
					try
					{
						logger.LogInformation("Checking database migrations (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
						if (context.Database.GetPendingMigrations().Any())
						{
							logger.LogInformation("Applying pending database migrations...");
							context.Database.Migrate();
							logger.LogInformation("Migrations applied successfully.");
						}
						break;
					}
					catch (Exception ex) when (attempt < maxRetries)
					{
						logger.LogWarning(ex, "Database not ready yet, retrying in 2 seconds...");
						Thread.Sleep(2000);
					}
				}
			}
		}

		using (var seedScope = app.ApplicationServices.CreateScope())
		{
			SeedDefaultUserAndConnection(seedScope.ServiceProvider, logger);
		}
	}

	/// <summary>
	/// Seeds a default admin account and local Redis connection if none exist.
	/// </summary>
	public static void SeedDefaultUserAndConnection(IServiceProvider services, ILogger? logger = null)
	{
		try
		{
			var context = services.GetRequiredService<ApplicationDbContext>();
			var passwordHasher = services.GetRequiredService<IPasswordHasher>();

			// 1. Seed default admin user
			var adminEmail = Email.Create("admin@redisgui.local").Value;
			var existingUser = context.Users.FirstOrDefault(u => u.Email.Value == adminEmail.Value);
			Guid adminId;

			if (existingUser == null)
			{
				var passwordHash = PasswordHash.Create("AdminPassword123!", passwordHasher).Value;
				var profile = UserProfile.Create("Admin", "RedisGUI").Value;
				var user = User.Create(adminEmail, passwordHash, profile, UserRole.Admin).Value;

				context.Users.Add(user);
				context.SaveChanges();
				adminId = user.Id;
				logger?.LogInformation("Default admin user seeded: admin@redisgui.local / AdminPassword123!");
			}
			else
			{
				adminId = existingUser.Id;
			}

			// 2. Seed default Redis connection if none exists
			var defaultConnId = Guid.Parse("ae3201ab-c3f1-4594-8f80-bbffd8d5eb65");
			var existingConn = context.Connections.FirstOrDefault(c => c.Id == defaultConnId);

			string? defaultHost = Environment.GetEnvironmentVariable("Redis__DefaultHost");
			string host = !string.IsNullOrWhiteSpace(defaultHost) ? defaultHost : "127.0.0.1";
			int port = int.TryParse(Environment.GetEnvironmentVariable("Redis__DefaultPort"), out int p) ? p : 6379;

			if (existingConn == null)
			{
				var connName = new ConnectionName("Local Redis");
				var connHost = new ConnectionHost(host);
				var connPort = new ConnectionPort(port);
				var dbNum = new DatabaseNumber(0);
				var ownerId = new ConnectionOwnerId(adminId);

				var connection = new AnonymousRedisConnection(
					defaultConnId,
					connName,
					connHost,
					connPort,
					dbNum,
					ownerId);

				context.Connections.Add(connection);
				context.SaveChanges();
				logger?.LogInformation("Default Redis connection seeded with stable ID {Id}: {Host}:{Port}", defaultConnId, host, port);
			}
			else
			{
				// Ensure host is up to date
				if (existingConn.ServerHost.Value != host)
				{
					existingConn.UpdateConnectionDetails(existingConn.ConnectionName, new ConnectionHost(host), existingConn.ServerPort, existingConn.DatabaseNumber);
					context.SaveChanges();
					logger?.LogInformation("Default Redis connection updated to {Host}:{Port}", host, port);
				}
			}
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Failed to seed default user/connection.");
		}
	}

	/// <summary>
	/// Seeds the database with fake data for testing purposes
	/// </summary>
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
			var dbNumber = new DatabaseNumber(faker.Random.Number(0, 15));
			var ownerId = new ConnectionOwnerId(System.Guid.NewGuid());
			var connectionResult = RedisConnection.Create(connectionName, connectionHost, connectionPort, dbNumber, ownerId, null);
			if (connectionResult.IsSuccess)
			{
				connections.Add(connectionResult.Value);
			}
		}

		context.Connections.AddRange(connections);
		context.SaveChanges();
	}
}
