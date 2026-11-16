using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.RunLoadTest;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Diagnostics;

[TestFixture]
public class LoadTestJobHandlerTests
{
	[Test]
	public async Task Validate_RejectsInvalidAndUnauthorizedJobs()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repo = new Mock<IRedisConnectionRepository>();
		repo.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var access = new Mock<IConnectionAccessService>();
		var handler = new LoadTestJobHandler(repo.Object, access.Object, Mock.Of<IMediator>());
		using var invalid = JsonDocument.Parse("{\"concurrency\":0}");
		Assert.That((await handler.ValidateAsync(connection.Id, invalid.RootElement, CancellationToken.None)).Error.Code, Is.EqualTo("Job.InvalidParameters"));
		using var valid = JsonDocument.Parse("{\"operationsCount\":100}");
		Assert.That((await handler.ValidateAsync(connection.Id, valid.RootElement, CancellationToken.None)).Error.Code, Is.EqualTo("Connection.Forbidden"));
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		connection.SetReadOnly(true);
		Assert.That((await handler.ValidateAsync(connection.Id, valid.RootElement, CancellationToken.None)).Error.Code, Is.EqualTo("Connection.ReadOnlyMode"));
	}
}
