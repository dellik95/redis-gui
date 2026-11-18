using FluentAssertions;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Queries.ScanRedisKeys;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class ScanRedisKeysQueryHandlerTests
{
	[TestCase(true)]
	[TestCase(false)]
	public async Task Handle_ChecksAccessBeforeScanning(bool allowed)
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(1), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(allowed);
		var keys = new Mock<IRedisKeyService>();
		keys.Setup(x => x.ScanKeysAsync(connection, "user:*", 1, "cursor", 50, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(new RedisKeyScanPage([], null)));
		var handler = new ScanRedisKeysQueryHandler(repository.Object, access.Object, keys.Object);

		var result = await handler.Handle(new ScanRedisKeysQuery(connection.Id, "user:*", null, "cursor", 50), CancellationToken.None);

		result.IsSuccess.Should().Be(allowed);
		if (!allowed) result.Error.Code.Should().Be("Connection.Forbidden");
		keys.Verify(x => x.ScanKeysAsync(connection, "user:*", 1, "cursor", 50, It.IsAny<CancellationToken>()), allowed ? Times.Once : Times.Never);
	}

	[Test]
	public async Task Handle_MissingConnectionDoesNotAuthorizeOrScan()
	{
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(new Error("Connection.NotFound", "Not found")));
		var access = new Mock<IConnectionAccessService>();
		var keys = new Mock<IRedisKeyService>();
		var handler = new ScanRedisKeysQueryHandler(repository.Object, access.Object, keys.Object);

		var result = await handler.Handle(new ScanRedisKeysQuery(Guid.NewGuid(), "*", 0, null, 100), CancellationToken.None);

		result.Error.Code.Should().Be("Connection.NotFound");
		access.Verify(x => x.CanAccessAsync(It.IsAny<RedisConnection>()), Times.Never);
		keys.VerifyNoOtherCalls();
	}
}
