using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Queries.GetAllRedisValues;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class GetAllRedisValuesQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisValueService> _connectionServiceMock = null!;
	private GetAllRedisValuesQueryHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_connectionServiceMock = new Mock<IRedisValueService>();
		_handler = new GetAllRedisValuesQueryHandler(
			_repositoryMock.Object,
			_connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_KeysExist_ReturnsPagedValues()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetAllRedisKeysQuery query = new(connectionId, "*", 10, 1);

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		List<RedisValue> values =
		[
			RedisValue.Create("key1", "value1", "String", null).Value!,
			RedisValue.Create("key2", "value2", "String", null).Value!
		];

		PagedRedisValues pagedResult = PagedRedisValues.Create(values, 2, 1, 10);

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_connectionServiceMock
			.Setup(x => x.GetAllValuesAsync(connection, "*", 10, 1, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(pagedResult));

		// Act
		Result<GetAllRedisValuesResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Values.Should().HaveCount(2);
		result.Value.TotalCount.Should().Be(2);
	}
}





