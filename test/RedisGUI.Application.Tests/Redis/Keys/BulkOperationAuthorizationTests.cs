using FluentResults;
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.BulkUpdateTtl;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Keys;

[TestFixture]
public class BulkOperationAuthorizationTests
{
	[Test]
	public async Task BulkMutations_DenyAccessBeforeCallingRedis()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(false);
		var keys = new Mock<IRedisKeyService>(MockBehavior.Strict);
		var values = new Mock<IRedisValueService>(MockBehavior.Strict);
		var selected = await new DeleteMultipleKeysCommandHandler(repository.Object, keys.Object, access.Object)
			.Handle(new DeleteMultipleKeysCommand(connection.Id, ["key"]), CancellationToken.None);
		var pattern = await new DeleteKeysByPatternCommandHandler(repository.Object, keys.Object, access.Object)
			.Handle(new DeleteKeysByPatternCommand(connection.Id, "*"), CancellationToken.None);
		var ttl = await new BulkUpdateTtlCommandHandler(repository.Object, values.Object, access.Object)
			.Handle(new BulkUpdateTtlCommand(connection.Id, ["key"], 10), CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(selected.GetDomainError().Code, Is.EqualTo("Connection.Forbidden"));
			Assert.That(pattern.GetDomainError().Code, Is.EqualTo("Connection.Forbidden"));
			Assert.That(ttl.GetDomainError().Code, Is.EqualTo("Connection.Forbidden"));
		});
	}
}
