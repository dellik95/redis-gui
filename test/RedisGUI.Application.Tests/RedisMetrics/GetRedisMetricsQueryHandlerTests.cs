using FluentAssertions;
using Moq;
using RedisGUI.Application.RedisMetrics.Queries;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.RedisMetrics.Abstractions;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.RedisMetrics;

[TestFixture]
public class GetRedisMetricsQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> repositoryMock = null!;
	private Mock<IMetricsCollector> metricsCollectorMock = null!;
	private GetRedisMetricsQueryHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		repositoryMock = new Mock<IRedisConnectionRepository>();
		metricsCollectorMock = new Mock<IMetricsCollector>();
		handler = new GetRedisMetricsQueryHandler(
			repositoryMock.Object,
			metricsCollectorMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionExists_ReturnsMetrics()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetRedisMetricsQuery query = new(connectionId);

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		RedisGUI.Domain.RedisMetrics.RedisMetrics metrics = new()
		{
			NetworkSpeedIn = 100,
			NetworkSpeedOut = 50,
			KeyCount = 1024,
			MemoryUsage = 1024,
			UptimeInSeconds = 10
		};

		repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		metricsCollectorMock
			.Setup(x => x.CollectMetrics(connection, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(metrics));

		// Act
		Result<RedisGUI.Domain.RedisMetrics.RedisMetrics> result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.KeyCount.Should().Be(1024);
		metricsCollectorMock.Verify(x => x.CollectMetrics(connection, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetRedisMetricsQuery query = new(connectionId);

		repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

		// Act
		Result<RedisGUI.Domain.RedisMetrics.RedisMetrics> result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		metricsCollectorMock.Verify(x => x.CollectMetrics(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()), Times.Never);
	}
}

