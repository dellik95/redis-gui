using FluentAssertions;
using Moq;
using RedisGUI.Application.Connections.GetConnectionById;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Connections;

[TestFixture]
public class GetConnectionByIdQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> repositoryMock = null!;
	private Mock<IPasswordDecrypt> passwordDecryptMock = null!;
	private GetConnectionByIdQueryHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		repositoryMock = new Mock<IRedisConnectionRepository>();
		passwordDecryptMock = new Mock<IPasswordDecrypt>();
		handler = new GetConnectionByIdQueryHandler(
			repositoryMock.Object,
			passwordDecryptMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionExists_ReturnsConnection()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetConnectionByIdQuery query = new(connectionId);
		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		passwordDecryptMock
			.Setup(x => x.DecryptPassword(It.IsAny<string>()))
			.Returns("decrypted");

		// Act
		Result<GetConnectionResponse> result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Id.Should().Be(connectionId);
		repositoryMock.Verify(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetConnectionByIdQuery query = new(connectionId);

		repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

		// Act
		Result<GetConnectionResponse> result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.NotFound);
	}
}

