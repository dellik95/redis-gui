using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Queries.GetKeyMemoryUsage;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class GetKeyMemoryUsageQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisDiagnosticsService> _diagnosticsServiceMock = null!;
	private GetKeyMemoryUsageQueryHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_diagnosticsServiceMock = new Mock<IRedisDiagnosticsService>();
		_handler = new GetKeyMemoryUsageQueryHandler(
			_repositoryMock.Object,
			_diagnosticsServiceMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetKeyMemoryUsageQuery query = new(connectionId, "user:100");

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.NotFound);
	}

	[Test]
	public async Task Handle_ValidRequest_ReturnsMemoryBytes()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetKeyMemoryUsageQuery query = new(connectionId, "user:100");

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_diagnosticsServiceMock
			.Setup(x => x.GetKeyMemoryUsageAsync(connection, "user:100", It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(1024L));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Key.Should().Be("user:100");
		result.Value.MemoryBytes.Should().Be(1024L);
	}
}
