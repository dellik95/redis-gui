using FluentResults;
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Messaging;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Messaging;

[TestFixture]
public class MessagingHandlerTests
{
	[TestCase(false, false, "Connection.Forbidden")]
	[TestCase(true, true, "Connection.Forbidden")]
	[TestCase(true, false, null)]
	public async Task Publish_ChecksAccessAndSafeMode(bool canAccess, bool readOnly, string? expectedError)
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		connection.SetReadOnly(readOnly);
		var repo = new Mock<IRedisConnectionRepository>();
		repo.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(canAccess);
		var messaging = new Mock<IRedisMessagingOperations>();
		messaging.Setup(x => x.PublishAsync(connection, "channel", "hello", It.IsAny<CancellationToken>())).ReturnsAsync(4);
		var handler = new PublishMessageCommandHandler(repo.Object, access.Object, messaging.Object);

		var result = await handler.Handle(new PublishMessageCommand(connection.Id, "channel", "hello"), CancellationToken.None);

		Assert.That(result.IsSuccess, Is.EqualTo(expectedError is null));
		if (expectedError is not null) Assert.That(result.GetDomainError().Code, Is.EqualTo(expectedError));
		else Assert.That(result.Value, Is.EqualTo(4));
		messaging.Verify(x => x.PublishAsync(connection, "channel", "hello", It.IsAny<CancellationToken>()), expectedError is null ? Times.Once : Times.Never);
	}

	[Test]
	public void StreamValidator_RejectsInvalidKey()
	{
		var result = new GetStreamQueryValidator().Validate(new GetStreamQuery(Guid.NewGuid(), " "));
		Assert.That(result.IsValid, Is.False);
	}
}
