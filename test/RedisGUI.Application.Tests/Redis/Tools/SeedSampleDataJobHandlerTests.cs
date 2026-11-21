using FluentResults;
using DispatchWeave;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.SeedSampleData;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Tools;

[TestFixture]
public class SeedSampleDataJobHandlerTests
{
	[Test]
	public async Task Validate_RejectsInvalidUnauthorizedAndReadOnlyRequests()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(connection));
		var access = new Mock<IConnectionAccessService>();
		var handler = new SeedSampleDataJobHandler(repository.Object, access.Object, Mock.Of<IDispatcher>());
		var invalid = JsonSerializer.SerializeToElement(new { keyCount = 10001 });
		var invalidResult = await handler.ValidateAsync(connection.Id, invalid, CancellationToken.None);
		Assert.That(invalidResult.IsFailed, Is.True);
		Assert.That(invalidResult.GetDomainError(), Is.InstanceOf<ValidationFailureError>());
		Assert.That(invalidResult.GetDomainError().Code, Is.EqualTo("Validation.Failed"));
		var valid = JsonSerializer.SerializeToElement(new { keyCount = 20, targetDatabases = new[] { 1 } });
		Assert.That((await handler.ValidateAsync(connection.Id, valid, CancellationToken.None)).GetDomainError().Code, Is.EqualTo("Connection.Forbidden"));
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		connection.SetReadOnly(true);
		Assert.That((await handler.ValidateAsync(connection.Id, valid, CancellationToken.None)).GetDomainError().Code, Is.EqualTo("Connection.ReadOnlyMode"));
	}
}
