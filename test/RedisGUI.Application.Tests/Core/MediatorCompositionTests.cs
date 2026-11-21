using DispatchWeave;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RedisGUI.Application.Connections.CheckConnection;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Queries.ScanRedisKeys;
using RedisGUI.Application.Redis.Commands.RenameKey;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Core;

[TestFixture]
public class MediatorCompositionTests
{
	[Test]
	public async Task SuccessfulAuditableCommand_IsRecordedByRegisteredPipeline()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var values = new Mock<IRedisValueService>();
		values.Setup(x => x.RenameKeyAsync(connection, "old", "new", false, null, It.IsAny<System.Threading.CancellationToken>()))
			.ReturnsAsync(Result.Success());
		var audit = new Mock<IAuditLogRepository>();
		var unit = new Mock<IUnitOfWork>();
		unit.Setup(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddApplication();
		services.AddSingleton(repository.Object);
		services.AddSingleton(values.Object);
		services.AddSingleton(audit.Object);
		services.AddSingleton(unit.Object);
		services.AddSingleton(Mock.Of<ICurrentUserService>());
		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var result = await scope.ServiceProvider.GetRequiredService<IDispatcher>().Send(new RenameKeyCommand(connection.Id, "old", "new"));

		Assert.That(result.IsSuccess, Is.True);
		audit.Verify(x => x.Add(It.IsAny<AuditLog>()), Times.Once);
		unit.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task InvalidQuery_IsValidatedBeforeConnectionLookup()
	{
		var repository = new Mock<IRedisConnectionRepository>();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddApplication();
		services.AddSingleton(repository.Object);
		services.AddSingleton(Mock.Of<IRedisKeyService>());
		services.AddSingleton(Mock.Of<IConnectionAccessService>());
		services.AddSingleton(Mock.Of<IAuditLogRepository>());
		services.AddSingleton(Mock.Of<IUnitOfWork>());
		services.AddSingleton(Mock.Of<ICurrentUserService>());
		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var result = await scope.ServiceProvider.GetRequiredService<IDispatcher>()
			.Send(new ScanRedisKeysQuery(System.Guid.NewGuid(), "", null, null, 0));

		Assert.That(result.Error, Is.InstanceOf<ValidationFailureError>());
		var failure = (ValidationFailureError)result.Error;
		Assert.That(failure.Errors.Keys, Does.Contain("Pattern"));
		Assert.That(failure.Errors.Keys, Does.Contain("Count"));
		repository.VerifyNoOtherCalls();
	}

	[Test]
	public async Task InvalidCommand_IsValidatedBeforeConnectionServiceRuns()
	{
		var connectionService = new Mock<IRedisConnectionService>();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddApplication();
		services.AddSingleton(connectionService.Object);
		services.AddSingleton(Mock.Of<IPasswordEncryptor>());
		services.AddSingleton(Mock.Of<IAuditLogRepository>());
		services.AddSingleton(Mock.Of<IUnitOfWork>());
		services.AddSingleton(Mock.Of<ICurrentUserService>());
		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var result = await scope.ServiceProvider.GetRequiredService<IDispatcher>().Send(new CheckConnectionCommand("", 0));

		Assert.That(result.IsFailure, Is.True);
		Assert.That(result.Error.Code, Is.EqualTo("Validation.Failed"));
		Assert.That(result.Error.Message, Does.Contain("validation"));
		Assert.That(result.Error, Is.InstanceOf<ValidationFailureError>());
		var validation = (ValidationFailureError)result.Error;
		Assert.That(validation.Errors.Keys, Does.Contain("Host"));
		Assert.That(validation.Errors.Keys, Does.Contain("Port"));
		connectionService.VerifyNoOtherCalls();
	}
}
