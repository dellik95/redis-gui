using FluentAssertions;
using Moq;
using RedisGUI.Application.Connections.CheckConnection;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Connections;

[TestFixture]
public class CheckConnectionCommandHandlerTests
{
	private Mock<IRedisConnectionService> connectionServiceMock = null!;
	private Mock<IPasswordEncryptor> passwordEncryptorMock = null!;
	private CheckConnectionCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionServiceMock = new Mock<IRedisConnectionService>();
		passwordEncryptorMock = new Mock<IPasswordEncryptor>();
		handler = new CheckConnectionCommandHandler(
			connectionServiceMock.Object,
			passwordEncryptorMock.Object);
	}

	[Test]
	public async Task Handle_ValidConnection_ReturnsSuccess()
	{
		// Arrange
		CheckConnectionCommand command = new(
			"localhost",
			6379,
			0,
			null,
			null);

		passwordEncryptorMock
			.Setup(x => x.EncryptPassword(It.IsAny<string>()))
			.Returns("encrypted");

		connectionServiceMock
			.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		// Act
		Result result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		connectionServiceMock.Verify(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_ConnectionFails_ReturnsFailure()
	{
		// Arrange
		CheckConnectionCommand command = new(
			"invalid-host",
			6379,
			0,
			null,
			null);

		passwordEncryptorMock
			.Setup(x => x.EncryptPassword(It.IsAny<string>()))
			.Returns("encrypted");

		connectionServiceMock
			.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure(DomainErrors.Connection.ConnectionNotEstablished));

		// Act
		Result result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
	}

	[Test]
	public async Task Handle_WithCredentials_EncryptsPassword()
	{
		// Arrange
		CheckConnectionCommand command = new(
			"localhost",
			6379,
			0,
			"username",
			"password");

		passwordEncryptorMock
			.Setup(x => x.EncryptPassword("password"))
			.Returns("encrypted_password");

		connectionServiceMock
			.Setup(x => x.CheckConnectionAsync(It.IsAny<RedisConnection>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		// Act
		Result result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		passwordEncryptorMock.Verify(x => x.EncryptPassword("password"), Times.Once);
	}
}

