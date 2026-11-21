using FluentResults;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Infrastructure.Redis.TypeHandlers;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class RedisValueServiceDatabaseTests
{
	[Test]
	public async Task GetAllValues_PassesSelectedDatabaseToKeyDiscovery()
	{
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Database test"),
			new ConnectionHost("localhost"), new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var keys = new Mock<IRedisKeyService>();
		keys.Setup(x => x.GetKeysAsync(connection, It.IsAny<CancellationToken>(), 1))
			.ReturnsAsync(Result.Ok<IEnumerable<string>>(Array.Empty<string>()));
		var service = new RedisValueService(Mock.Of<IRedisConnectionManager>(), keys.Object,
			Mock.Of<IRedisTypeHandlerFactory>(), Mock.Of<IRedisLoadTestService>(), Mock.Of<IRedisSeedDataService>(),
			Mock.Of<IRedisCliService>(), Mock.Of<IRedisDiagnosticsService>(), NullLogger<RedisValueService>.Instance);

		var result = await service.GetAllValuesAsync(connection, database: 1);

		Assert.That(result.IsSuccess, Is.True);
		keys.Verify(x => x.GetKeysAsync(connection, It.IsAny<CancellationToken>(), 1), Times.Once);
	}
}
