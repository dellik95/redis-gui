using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Keys;

[TestFixture]
public class StreamDeleteKeysByPatternTests
{
	[Test]
	public async Task Cancellation_ReturnsAndAuditsConfirmedDeletionCount()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		var keys = new Mock<IRedisKeyService>();
		using var cancellation = new CancellationTokenSource();
		keys.Setup(x => x.DeleteKeysByPatternAsync(connection, "prefix:*", It.IsAny<CancellationToken>(), 1, It.IsAny<Func<int, Task>>()))
			.Returns(async (RedisConnection _, string _, CancellationToken token, int? _, Func<int, Task> report) =>
			{
				await report(3);
				cancellation.Cancel();
				token.ThrowIfCancellationRequested();
				return Result.Success(3);
			});
		var handler = new StreamDeleteKeysByPatternCommandHandler(repository.Object, access.Object, keys.Object);
		var logs = new Mock<IAuditLogRepository>();
		var unit = new Mock<IUnitOfWork>();
		var pipeline = new AuditingBehavior<StreamDeleteKeysByPatternCommand, Result<StreamDeleteKeysByPatternResult>>(
			logs.Object, unit.Object, Mock.Of<ICurrentUserService>());
		var command = new StreamDeleteKeysByPatternCommand(connection.Id, "prefix:*", 1, _ => Task.CompletedTask);
		var result = await pipeline.Handle(command, cancellation.Token, () => handler.Handle(command, cancellation.Token));
		Assert.That(result.Value.Cancelled, Is.True);
		Assert.That(result.Value.DeletedCount, Is.EqualTo(3));
		logs.Verify(x => x.Add(It.Is<AuditLog>(log => log.EntityId == connection.Id &&
			log.Metadata.NewValue == "Deleted 3 matching keys; cancelled: True; failed: False.")), Times.Once);
		unit.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
	}
}
