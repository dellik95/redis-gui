using FluentAssertions;
using Moq;
using RedisGUI.Application.Connections.CreateConnection;
using RedisGUI.Application.Core.Services;
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
public class CreateConnectionCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<IPasswordEncryptor> passwordEncryptorMock = null!;
	private Mock<IUnitOfWork> unitOfWorkMock = null!;
	private Mock<IRedisConnectionService> connectionServiceMock = null!;
	private Mock<ICurrentUserService> currentUserServiceMock = null!;
	private CreateConnectionCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
		passwordEncryptorMock = new Mock<IPasswordEncryptor>();
		unitOfWorkMock = new Mock<IUnitOfWork>();
		connectionServiceMock = new Mock<IRedisConnectionService>();
		currentUserServiceMock = new Mock<ICurrentUserService>();
		handler = new CreateConnectionCommandHandler(
			connectionRepositoryMock.Object,
			passwordEncryptorMock.Object,
			unitOfWorkMock.Object,
			connectionServiceMock.Object,
			currentUserServiceMock.Object);
	}

	[Test]
	public async Task Handle_ValidRequest_ReturnsConnectionId()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		CreateConnectionCommand command = new(
			"Test Connection",
			"localhost",
			6379,
			0,
			null,
			null);

		currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
		passwordEncryptorMock
			.Setup(x => x.EncryptPassword(It.IsAny<string>()))
			.Returns("encrypted");

		// Connection will be created by handler

		connectionServiceMock
			.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(1));

		// Act
		Result<Guid> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBe(Guid.Empty);
		connectionRepositoryMock.Verify(x => x.Add(It.IsAny<RedisConnection>()), Times.Once);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_NoAuthenticatedUser_ReturnsUnauthorized()
	{
		// Arrange
		CreateConnectionCommand command = new(
			"Test Connection",
			"localhost",
			6379,
			0,
			null,
			null);

		currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

		// Act
		Result<Guid> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.User.Unauthorized);
		connectionRepositoryMock.Verify(x => x.Add(It.IsAny<RedisConnection>()), Times.Never);
	}

	[Test]
	public async Task Handle_ConnectionCheckFails_ReturnsFailure()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		CreateConnectionCommand command = new(
			"Test Connection",
			"localhost",
			6379,
			0,
			null,
			null);

		currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
		connectionServiceMock
			.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure(DomainErrors.Connection.ConnectionNotEstablished));

		// Act
		Result<Guid> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.ConnectionNotEstablished);
	}

	[Test]
	public async Task Handle_WithCredentials_EncryptsPassword()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		CreateConnectionCommand command = new(
			"Test Connection",
			"localhost",
			6379,
			0,
			"username",
			"password");

		currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
		passwordEncryptorMock
			.Setup(x => x.EncryptPassword("password"))
			.Returns("encrypted_password");

		connectionServiceMock
			.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(1));

		// Act
		Result<Guid> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		passwordEncryptorMock.Verify(x => x.EncryptPassword("password"), Times.Once);
	}
}

