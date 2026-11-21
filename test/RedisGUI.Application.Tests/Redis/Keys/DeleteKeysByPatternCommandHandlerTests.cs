using FluentResults;
using FluentAssertions;
using RedisGUI.Application.Core.Services;
using Moq;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class DeleteKeysByPatternCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<IRedisKeyService> connectionServiceMock = null!;
	private Mock<IConnectionAccessService> access = null!;
	private DeleteKeysByPatternCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
		connectionServiceMock = new Mock<IRedisKeyService>();
		access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(It.IsAny<RedisConnection>())).ReturnsAsync(true);
		handler = new DeleteKeysByPatternCommandHandler(
			connectionRepositoryMock.Object,
			connectionServiceMock.Object, access.Object);
	}

	[Test]
	public async Task Handle_ValidCommand_ReturnsSuccessWithDeletedCount()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var pattern = "user:*";
		var command = new DeleteKeysByPatternCommand(connectionId, pattern);

		var connection = RedisConnection.Create(
			  new ConnectionName("Test Connection"),
			  new ConnectionHost("localhost"),
			  new ConnectionPort(6379),
			  new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(connection));

		connectionServiceMock
			.Setup(x => x.DeleteKeysByPatternAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(5));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.DeletedCount.Should().Be(5);
		result.Value.Pattern.Should().Be(pattern);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var command = new DeleteKeysByPatternCommand(connectionId, "test:*");

		var error = new DomainError("Connection.NotFound", "Connection not found");
		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Fail<RedisConnection>(error));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailed.Should().BeTrue();
		result.GetDomainError().Should().Be(error);
	}

	[Test]
	public async Task Handle_DeleteFails_ReturnsFailure()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var command = new DeleteKeysByPatternCommand(connectionId, "test:*");

		var connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(connection));

		var error = new DomainError("Redis.DeleteFailed", "Delete operation failed");
		connectionServiceMock
			.Setup(x => x.DeleteKeysByPatternAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Fail<int>(error));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailed.Should().BeTrue();
		result.GetDomainError().Should().Be(error);
	}
}





