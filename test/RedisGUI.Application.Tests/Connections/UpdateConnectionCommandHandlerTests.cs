using FluentAssertions;
using Moq;
using RedisGUI.Application.Connections.UpdateConnection;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Connections;

[TestFixture]
public class UpdateConnectionCommandHandlerTests
{
private Mock<IRedisConnectionRepository> _repositoryMock = null!;
private Mock<IPasswordEncryptor> _passwordEncryptorMock = null!;
private Mock<IUnitOfWork> _unitOfWorkMock = null!;
private Mock<IRedisConnectionService> _connectionServiceMock = null!;
private UpdateConnectionCommandHandler _handler = null!;

[SetUp]
public void Setup()
{
_repositoryMock = new Mock<IRedisConnectionRepository>();
_passwordEncryptorMock = new Mock<IPasswordEncryptor>();
_unitOfWorkMock = new Mock<IUnitOfWork>();
_connectionServiceMock = new Mock<IRedisConnectionService>();
_handler = new UpdateConnectionCommandHandler(
_repositoryMock.Object,
_passwordEncryptorMock.Object,
_unitOfWorkMock.Object,
_connectionServiceMock.Object);
}

[Test]
public async Task Handle_ValidUpdate_UpdatesAndSaves()
{
// Arrange
Guid connectionId = Guid.NewGuid();
UpdateConnectionCommand command = new(
connectionId,
"Updated Name",
"updated.host",
6380,
1,
null,
null);

RedisConnection existingConnection = RedisConnection.Create(
new ConnectionName("Original Name"),
new ConnectionHost("original.host"),
new ConnectionPort(6379),
new DatabaseNumber(0),
ConnectionOwnerId.None()).Value!;

_repositoryMock
.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
.ReturnsAsync(Result.Success(existingConnection));

_connectionServiceMock
.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
.ReturnsAsync(Result.Success());

_unitOfWorkMock
.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
.ReturnsAsync(Result.Success(1));

// Act
Result result = await _handler.Handle(command, CancellationToken.None);

// Assert
result.IsSuccess.Should().BeTrue();
_repositoryMock.Verify(x => x.Update(existingConnection), Times.Once);
_unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Test]
public async Task Handle_ConnectionNotFound_ReturnsFailure()
{
// Arrange
Guid connectionId = Guid.NewGuid();
UpdateConnectionCommand command = new(
connectionId,
"Updated Name",
"updated.host",
6380,
1,
null,
null);

_repositoryMock
.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

// Act
Result result = await _handler.Handle(command, CancellationToken.None);

// Assert
result.IsFailure.Should().BeTrue();
result.Error.Should().Be(DomainErrors.Connection.NotFound);
}

[Test]
public async Task Handle_ConcurrencyConflict_ReturnsFailure()
{
// Arrange
Guid connectionId = Guid.NewGuid();
UpdateConnectionCommand command = new(
connectionId,
"Updated Name",
"updated.host",
6380,
1,
null,
null);

RedisConnection existingConnection = RedisConnection.Create(
new ConnectionName("Original Name"),
new ConnectionHost("original.host"),
new ConnectionPort(6379),
new DatabaseNumber(0),
ConnectionOwnerId.None()).Value!;

_repositoryMock
.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
.ReturnsAsync(Result.Success(existingConnection));

_connectionServiceMock
.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
.ReturnsAsync(Result.Success());

_unitOfWorkMock
.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
.ReturnsAsync(Result.Failure<int>(DomainErrors.Storage.ConcurrencyConflict));

// Act
Result result = await _handler.Handle(command, CancellationToken.None);

// Assert
result.IsFailure.Should().BeTrue();
result.Error.Should().Be(DomainErrors.Storage.ConcurrencyConflict);
_repositoryMock.Verify(x => x.Update(existingConnection), Times.Once);
_unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}
}
