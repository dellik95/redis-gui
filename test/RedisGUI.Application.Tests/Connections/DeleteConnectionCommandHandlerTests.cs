using FluentAssertions;
using Moq;
using RedisGUI.Application.Connections.DeleteConnection;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Connections;

[TestFixture]
public class DeleteConnectionCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> repositoryMock = null!;
	private Mock<IUnitOfWork> unitOfWorkMock = null!;
	private DeleteConnectionCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		repositoryMock = new Mock<IRedisConnectionRepository>();
		unitOfWorkMock = new Mock<IUnitOfWork>();
		handler = new DeleteConnectionCommandHandler(
			repositoryMock.Object,
			unitOfWorkMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionExists_DeletesAndSaves()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		DeleteConnectionCommand command = new(connectionId);

		repositoryMock
			.Setup(x => x.DeleteAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(1));

		// Act
		Result result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		repositoryMock.Verify(x => x.DeleteAsync(connectionId, It.IsAny<CancellationToken>()), Times.Once);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		DeleteConnectionCommand command = new(connectionId);

		repositoryMock
			.Setup(x => x.DeleteAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure(RedisGUI.Domain.Errors.DomainErrors.Connection.NotFound));

		// Act
		Result result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}
}

