using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Commands.SeedSampleData;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class SeedSampleDataCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisSeedDataService> _seedDataServiceMock = null!;
	private SeedSampleDataCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_seedDataServiceMock = new Mock<IRedisSeedDataService>();
		_handler = new SeedSampleDataCommandHandler(_repositoryMock.Object, _seedDataServiceMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		Guid connectionId = Guid.NewGuid();
		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

		var result = await _handler.Handle(new SeedSampleDataCommand(connectionId), CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.NotFound);
	}

	[Test]
	public async Task Handle_ReadOnlyConnection_ReturnsReadOnlyError()
	{
		Guid connectionId = Guid.NewGuid();
		var conn = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			new ConnectionOwnerId(Guid.NewGuid())).Value;
		conn.SetReadOnly(true);

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(conn));

		var result = await _handler.Handle(new SeedSampleDataCommand(connectionId), CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.ReadOnlyMode);
	}

	[Test]
	public async Task Handle_ValidConnection_SeedsDataSuccessfully()
	{
		Guid connectionId = Guid.NewGuid();
		var conn = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			new ConnectionOwnerId(Guid.NewGuid())).Value;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(conn));

		var expectedResult = new SeedDataResult
		{
			TotalKeysCreated = 16,
			TargetDatabases = [0]
		};

		_seedDataServiceMock
			.Setup(x => x.SeedSampleDataAsync(conn, It.IsAny<SeedSampleDataOptions?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(expectedResult));

		var result = await _handler.Handle(new SeedSampleDataCommand(connectionId), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.TotalKeysCreated.Should().Be(16);
	}
}
