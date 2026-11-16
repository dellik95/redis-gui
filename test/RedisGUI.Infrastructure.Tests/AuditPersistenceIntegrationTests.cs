using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RedisGUI.Domain.Audit;
using RedisGUI.Infrastructure.Persistence;
using RedisGUI.Infrastructure.Persistence.Repositories;

namespace RedisGUI.Infrastructure.Tests;

[TestFixture]
public class AuditPersistenceIntegrationTests
{
	[Test]
	public async Task Search_PersistsAndCombinesFiltersBeforePagination()
	{
		string? connection = Environment.GetEnvironmentVariable("REDISGUI_TEST_MYSQL");
		if (string.IsNullOrEmpty(connection)) Assert.Ignore("Set REDISGUI_TEST_MYSQL to a disposable test database.");
		var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseMySQL(connection).Options;
		await using var context = new ApplicationDbContext(options);
		await context.Database.EnsureCreatedAsync();
		var repository = new AuditLogRepository(context);
		Guid user = Guid.NewGuid();
		Guid entity = Guid.NewGuid();
		try
		{
			for (int i = 0; i < 3; i++) repository.Add(AuditLog.Create(user, AuditAction.Update, "Connection", entity, AuditMetadata.Create("127.0.0.1", "test")));
			repository.Add(AuditLog.Create(user, AuditAction.Update, "Redis", entity, AuditMetadata.Create("127.0.0.1", "test")));
			Assert.That((await context.SaveChangesAsync()).IsSuccess, Is.True);
			context.ChangeTracker.Clear();
			var (items, count) = await repository.SearchAsync(user, "Connection", entity, DateTime.UtcNow.AddDays(-1), null, 1, 1);
			Assert.That(count, Is.EqualTo(3));
			Assert.That(items, Has.Count.EqualTo(1));
			Assert.That(items[0].Metadata.IpAddress, Is.EqualTo("127.0.0.1"));
		}
		finally
		{
			await context.AuditLogs.Where(log => log.UserId == user).ExecuteDeleteAsync();
		}

	}
}
