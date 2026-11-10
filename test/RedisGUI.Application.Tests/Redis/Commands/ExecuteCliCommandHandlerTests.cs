using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.ExecuteCli;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class ExecuteCliCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisCliService> _cliServiceMock = null!;
	private ExecuteCliCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_cliServiceMock = new Mock<IRedisCliService>();
		_handler = new ExecuteCliCommandHandler(
			_repositoryMock.Object,
			_cliServiceMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		ExecuteCliCommand command = new(connectionId, "PING");

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.NotFound);
	}

	[Test]
	public async Task Handle_ValidCommand_ReturnsCliOutput()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		ExecuteCliCommand command = new(connectionId, "PING");

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_cliServiceMock
			.Setup(x => x.ExecuteCliCommandAsync(connection, "PING", null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success("PONG"));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Output.Should().Be("PONG");
		result.Value.Success.Should().BeTrue();
	}
}
