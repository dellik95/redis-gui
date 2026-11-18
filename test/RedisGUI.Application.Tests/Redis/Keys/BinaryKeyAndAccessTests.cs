using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Connections.GetAllConnections;
using RedisGUI.Application.Connections.GetConnectionById;
using RedisGUI.Application.Connections.GetConnectionHealth;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Queries.GetRedisValue;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Keys;

[TestFixture]
public class BinaryKeyAndAccessTests
{
	[Test]
	public async Task GetRedisValue_ResolvesBinaryBase64KeyAndPreservesIdentityMetadata()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		byte[] rawBytes = [0xFF, 0x00, 0x80, 0x41];
		string base64 = Convert.ToBase64String(rawBytes);

		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		var valueService = new Mock<IRedisValueService>();
		valueService.Setup(x => x.GetValueByIdentityAsync(connection, It.Is<string>(k => !string.IsNullOrEmpty(k)), base64, It.IsAny<CancellationToken>(), null))
			.ReturnsAsync((RedisConnection _, string k, string? _, CancellationToken _, int? _) =>
				RedisValue.Create(k, "hello", "String"));

		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);

		var handler = new GetRedisValueQueryHandler(repository.Object, valueService.Object, access.Object);
		var result = await handler.Handle(new GetRedisValueQuery(connection.Id, string.Empty, null, base64), CancellationToken.None);

		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value.KeyBase64, Is.EqualTo(base64));
		Assert.That(result.Value.IsBinary, Is.True);
	}

	[Test]
	public async Task ConnectionHandlers_EnforceOwnershipAndIncludeConfirmedServerMode()
	{
		var allowedConn = RedisConnection.Create(new ConnectionName("Allowed"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var blockedConn = RedisConnection.Create(new ConnectionName("Blocked"), new ConnectionHost("localhost"),
			new ConnectionPort(6380), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;

		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(allowedConn.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(allowedConn));
		repository.Setup(x => x.GetConnectionByIdAsync(blockedConn.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(blockedConn));
		repository.Setup(x => x.GetAsync(null, false, 1, 10))
			.ReturnsAsync(Result.Success(new PagedResult<RedisConnection>([allowedConn, blockedConn], 2, 1, 10)));

		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(allowedConn)).ReturnsAsync(true);
		access.Setup(x => x.CanAccessAsync(blockedConn)).ReturnsAsync(false);

		var topology = new RedisTopologyRegistry();
		topology.SetMode(allowedConn.Id, RedisServerMode.Cluster);

		var allHandler = new GetAllConnectionsQueryHandler(repository.Object, Mock.Of<IPasswordDecrypt>(), topology, access.Object);
		var allResult = await allHandler.Handle(new GetAllConnectionsQuery(1, 10), CancellationToken.None);
		Assert.That(allResult.IsSuccess, Is.True);
		var items = allResult.Value.ToList();
		Assert.That(items, Has.Count.EqualTo(1));
		Assert.That(items[0].Id, Is.EqualTo(allowedConn.Id));
		Assert.That(items[0].ServerMode, Is.EqualTo("Cluster"));

		var byIdHandler = new GetConnectionByIdQueryHandler(repository.Object, Mock.Of<IPasswordDecrypt>(), topology, access.Object);
		var denied = await byIdHandler.Handle(new GetConnectionByIdQuery(blockedConn.Id), CancellationToken.None);
		Assert.That(denied.IsFailure, Is.True);
		Assert.That(denied.Error.Code, Is.EqualTo("Connection.Forbidden"));

		var monitor = new Mock<IConnectionHealthMonitor>();
		monitor.Setup(x => x.GetHealth(allowedConn.Id)).Returns(ConnectionHealth.Healthy(allowedConn.Id, 4.2));
		var healthHandler = new GetConnectionHealthQueryHandler(monitor.Object, repository.Object, access.Object, topology);
		var healthResult = await healthHandler.Handle(new GetConnectionHealthQuery(allowedConn.Id), CancellationToken.None);
		Assert.That(healthResult.IsSuccess, Is.True);
		Assert.That(healthResult.Value.ServerMode, Is.EqualTo("Cluster"));
	}
}
