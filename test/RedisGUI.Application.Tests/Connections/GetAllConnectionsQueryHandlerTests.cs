using FluentAssertions;
using Moq;
using RedisGUI.Application.Connections.GetAllConnections;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Connections;

[TestFixture]
public class GetAllConnectionsQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> repositoryMock = null!;
	private Mock<IPasswordDecrypt> passwordDecryptMock = null!;
	private GetAllConnectionsQueryHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		repositoryMock = new Mock<IRedisConnectionRepository>();
		passwordDecryptMock = new Mock<IPasswordDecrypt>();
		handler = new GetAllConnectionsQueryHandler(
			repositoryMock.Object,
			passwordDecryptMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionsExist_ReturnsConnections()
	{
		// Arrange
		GetAllConnectionsQuery query = new(1, 10);
		List<RedisConnection> connections =
		[
			RedisConnection.Create(
				new ConnectionName("Connection 1"),
				new ConnectionHost("localhost"),
				new ConnectionPort(6379),
				new DatabaseNumber(0),
				ConnectionOwnerId.None()).Value!,
			RedisConnection.Create(
				new ConnectionName("Connection 2"),
				new ConnectionHost("localhost"),
				new ConnectionPort(6380),
				new DatabaseNumber(1),
				ConnectionOwnerId.None()).Value!
		];

		PagedResult<RedisConnection> pagedResult = new(
			connections,
			2,
			1,
			10);

		repositoryMock
			.Setup(x => x.GetAsync(null, false, 1, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(pagedResult));

		passwordDecryptMock
			.Setup(x => x.DecryptPassword(It.IsAny<string>()))
			.Returns("decrypted");

		// Act
		Result<IEnumerable<GetConnectionResponse>> result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		repositoryMock.Verify(x => x.GetAsync(null, false, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_NoConnections_ReturnsEmptyList()
	{
		// Arrange
		GetAllConnectionsQuery query = new(1, 10);
		PagedResult<RedisConnection> pagedResult = new(
			[],
			0,
			1,
			10);

		repositoryMock
			.Setup(x => x.GetAsync(null, false, 1, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(pagedResult));

		// Act
		Result<IEnumerable<GetConnectionResponse>> result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Test]
	public async Task Handle_RepositoryFails_ReturnsFailure()
	{
		// Arrange
		GetAllConnectionsQuery query = new(1, 10);
		repositoryMock
			.Setup(x => x.GetAsync(null, false, 1, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<PagedResult<RedisConnection>>(DomainErrors.Storage.ItemNotFound));

		// Act
		Result<IEnumerable<GetConnectionResponse>> result = await handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
	}
}

