using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Queries.GetRedisValue;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class GetRedisValueQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisValueService> _connectionServiceMock = null!;
	private GetRedisValueQueryHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_connectionServiceMock = new Mock<IRedisValueService>();
		_handler = new GetRedisValueQueryHandler(
			_repositoryMock.Object,
			_connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_KeyExists_ReturnsValue()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		string key = "test:key";
		GetRedisValueQuery query = new(connectionId, key);

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		RedisValue redisValue = RedisValue.Create(key, "test-value", "String", TimeSpan.FromMinutes(5)).Value!;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_connectionServiceMock
			.Setup(x => x.GetValueAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(redisValue));

		// Act
		Result<GetRedisValueResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Key.Should().Be(key);
		result.Value.Value.Should().Be("test-value");
	}

	[Test]
	public async Task Handle_KeyNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		string key = "nonexistent:key";
		GetRedisValueQuery query = new(connectionId, key);

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_connectionServiceMock
			.Setup(x => x.GetValueAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisValue>(DomainErrors.Redis.KeyNotFound));

		// Act
		Result<GetRedisValueResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
	}
}





