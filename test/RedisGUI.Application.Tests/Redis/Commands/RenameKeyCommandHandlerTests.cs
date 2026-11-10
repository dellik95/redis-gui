using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.RenameKey;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class RenameKeyCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisValueService> _valueServiceMock = null!;
	private RenameKeyCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_valueServiceMock = new Mock<IRedisValueService>();
		_handler = new RenameKeyCommandHandler(_repositoryMock.Object, _valueServiceMock.Object);
	}

	[Test]
	public async Task Handle_ReadOnlyConnection_ReturnsFailure()
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

		var result = await _handler.Handle(new RenameKeyCommand(connectionId, "old", "new"), CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.ReadOnlyMode);
	}

	[Test]
	public async Task Handle_ValidCommand_RenamesKeySuccessfully()
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

		_valueServiceMock
			.Setup(x => x.RenameKeyAsync(conn, "old", "new", false, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		var result = await _handler.Handle(new RenameKeyCommand(connectionId, "old", "new"), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
	}
}
