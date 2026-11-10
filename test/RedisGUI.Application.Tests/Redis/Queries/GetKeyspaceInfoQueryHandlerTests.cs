using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Queries.GetKeyspaceInfo;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class GetKeyspaceInfoQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisDiagnosticsService> _diagnosticsServiceMock = null!;
	private GetKeyspaceInfoQueryHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_diagnosticsServiceMock = new Mock<IRedisDiagnosticsService>();
		_handler = new GetKeyspaceInfoQueryHandler(
			_repositoryMock.Object,
			_diagnosticsServiceMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetKeyspaceInfoQuery query = new(connectionId);

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
	public async Task Handle_ValidRequest_ReturnsKeyspaceInfo()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetKeyspaceInfoQuery query = new(connectionId);

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		var expectedData = new Dictionary<int, long> { [0] = 42L, [1] = 10L };
		_diagnosticsServiceMock
			.Setup(x => x.GetKeyspaceInfoAsync(connection, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(expectedData));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Databases[0].Should().Be(42L);
		result.Value.Databases[1].Should().Be(10L);
	}
}
