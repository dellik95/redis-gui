using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Persistence;

namespace RedisGUI.Infrastructure.Tests;

[TestFixture]
public class MySqlProviderCompatibilityTests
{
	[Test]
	public void MySqlProvider_CreatesApplicationModel()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseMySQL("Server=localhost;Database=redisgui;User=test;Password=test")
			.ConfigureWarnings(warnings => warnings.Throw(CoreEventId.MappedPropertyIgnoredWarning))
			.Options;
		using var context = new ApplicationDbContext(options);
		Assert.That(context.Model.FindEntityType(typeof(RedisGUI.Domain.Users.User)), Is.Not.Null);
		Assert.That(context.Model.FindEntityType(typeof(RedisConnection))!.FindProperty(nameof(RedisConnection.OwnerId)), Is.Null);
	}

	[Test]
	public async Task MySqlProvider_CreatesSchemaAndQueriesUsers()
	{
		var connectionString = Environment.GetEnvironmentVariable("REDISGUI_TEST_MYSQL");
		if (string.IsNullOrEmpty(connectionString))
		{
			Assert.Ignore("Set REDISGUI_TEST_MYSQL to run the MySQL integration check.");
		}

		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseMySQL(connectionString)
			.Options;
		await using var context = new ApplicationDbContext(options);
		await context.Database.EnsureCreatedAsync();
		Assert.That(await context.Users.CountAsync(), Is.Zero);
	}
}
