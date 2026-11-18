using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Persistence;
using RedisGUI.Infrastructure.Persistence.Cache;

namespace RedisGUI.Infrastructure.Tests.Persistence;

[TestFixture]
public class CacheKeyProviderTests
{
	[Test]
	public void GetCacheKey_DiffersWhenParameterValuesDiffer()
	{
		var options = Options.Create(new CacheConfiguration { KeyPrefix = "test" });
		var provider = new CacheKeyProvider(options, NullLogger<CacheKeyProvider>.Instance);
		using var dbContext = new ApplicationDbContext(
			new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
				.Options);
		var tables = new SortedSet<string>(StringComparer.Ordinal) { "AnonymousRedisConnection" };

		using DbCommand cmd1 = CreateParameterizedCommand("SELECT * FROM AnonymousRedisConnection WHERE Id = @p0", Guid.NewGuid());
		using DbCommand cmd2 = CreateParameterizedCommand("SELECT * FROM AnonymousRedisConnection WHERE Id = @p0", Guid.NewGuid());

		string key1 = provider.GetCacheKey(cmd1, dbContext, tables);
		string key2 = provider.GetCacheKey(cmd2, dbContext, tables);

		Assert.That(key1, Is.Not.EqualTo(key2));
	}

	[Test]
	public void GetCacheKey_MatchesForIdenticalSqlAndParameters()
	{
		var options = Options.Create(new CacheConfiguration { KeyPrefix = "test" });
		var provider = new CacheKeyProvider(options, NullLogger<CacheKeyProvider>.Instance);
		using var dbContext = new ApplicationDbContext(
			new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
				.Options);
		var tables = new SortedSet<string>(StringComparer.Ordinal) { "AnonymousRedisConnection" };
		var sharedId = Guid.Parse("d33b6393-c908-42b0-b6fd-d92caeca2068");

		using DbCommand cmd1 = CreateParameterizedCommand("SELECT * FROM AnonymousRedisConnection WHERE Id = @p0", sharedId);
		using DbCommand cmd2 = CreateParameterizedCommand("SELECT * FROM AnonymousRedisConnection WHERE Id = @p0", sharedId);

		string key1 = provider.GetCacheKey(cmd1, dbContext, tables);
		string key2 = provider.GetCacheKey(cmd2, dbContext, tables);

		Assert.That(key1, Is.EqualTo(key2));
	}

	private static DbCommand CreateParameterizedCommand(string sql, Guid id)
	{
		var command = new MySqlCommand(sql);
		var param = command.CreateParameter();
		param.ParameterName = "@p0";
		param.DbType = DbType.Guid;
		param.Value = id;
		command.Parameters.Add(param);
		return command;
	}
}
