using FluentResults;
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.RunLoadTest;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Diagnostics;

[TestFixture]
public class RunStreamingLoadTestCommandTests
{
	[TestCase(false, false, "Connection.Forbidden")]
	[TestCase(true, true, "Connection.ReadOnlyMode")]
	[TestCase(true, false, null)]
	public async Task Handle_RequiresAccessAndRespectsSafeMode(bool allowed, bool readOnly, string? error)
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		connection.SetReadOnly(readOnly);
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(allowed);
		var service = new Mock<IRedisLoadTestService>();
		var request = new RedisLoadTestRequest();
		Func<RedisLoadTestProgress, Task> progress = _ => Task.CompletedTask;
		service.Setup(x => x.RunStreamingLoadTestAsync(connection, request, progress, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisLoadTestResult()));
		var handler = new RunStreamingLoadTestCommandHandler(repository.Object, access.Object, service.Object);

		var result = await handler.Handle(new RunStreamingLoadTestCommand(connection.Id, request, progress), CancellationToken.None);

		Assert.That(result.IsSuccess, Is.EqualTo(error is null));
		if (error is not null) Assert.That(result.GetDomainError().Code, Is.EqualTo(error));
		service.Verify(x => x.RunStreamingLoadTestAsync(connection, request, progress, It.IsAny<CancellationToken>()), error is null ? Times.Once : Times.Never);
	}
}
