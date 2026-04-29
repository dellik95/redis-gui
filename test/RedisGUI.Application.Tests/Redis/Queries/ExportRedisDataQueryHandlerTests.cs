using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Queries.ExportRedisData;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class ExportRedisDataQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<IRedisValueService> connectionServiceMock = null!;
	private ExportRedisDataQueryHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
		connectionServiceMock = new Mock<IRedisValueService>();
		handler = new ExportRedisDataQueryHandler(
			connectionRepositoryMock.Object,
			connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_JsonFormat_ReturnsJsonData()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var query = new ExportRedisDataQuery(connectionId, "*", ExportFormat.Json);

		var connection = RedisConnection.Create(
			  new ConnectionName("Test Connection"),
			  new ConnectionHost("localhost"),
			  new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		var redisValues = new List<RedisValue>
		{
			RedisValue.Create("key1", "value1", "String").Value,
			RedisValue.Create("key2", "value2", "String", TimeSpan.FromSeconds(60)).Value
		};

		var pagedValues = PagedRedisValues.Create(redisValues, 2, 1, 10);

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		connectionServiceMock
			.Setup(x => x.GetAllValuesAsync(connection, "*", null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(pagedValues));

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Data.Should().Contain("key1");
		result.Value.Data.Should().Contain("value1");
		result.Value.ContentType.Should().Be("application/json");
		result.Value.FileName.Should().EndWith(".json");
	}

	[Test]
	public async Task Handle_CsvFormat_ReturnsCsvData()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var query = new ExportRedisDataQuery(connectionId, "*", ExportFormat.Csv);

		var connection = RedisConnection.Create(
			  new ConnectionName("Test Connection"),
			  new ConnectionHost("localhost"),
			  new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		var redisValues = new List<RedisValue>
		{
			RedisValue.Create("key1", "value1", "String").Value
		};

		var pagedValues = PagedRedisValues.Create(redisValues, 1, 1, 10);

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		connectionServiceMock
			.Setup(x => x.GetAllValuesAsync(connection, "*", null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(pagedValues));

		// Act
		var result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Data.Should().Contain("Key,Value,Type,TTL");
		result.Value.Data.Should().Contain("key1");
		result.Value.ContentType.Should().Be("text/csv");
		result.Value.FileName.Should().EndWith(".csv");
	}
}




