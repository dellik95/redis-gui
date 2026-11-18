using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.ImportRedisData;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class ImportRedisDataJobHandlerTests
{
	[Test]
	public async Task CancelledJob_ReturnsAndAuditsConfirmedPartialCounts()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		var snapshots = new Mock<IRedisSnapshotService>();
		snapshots.Setup(x => x.SetLegacyStringAsync(connection, It.IsAny<string>(), It.IsAny<string>(), null, false, 0, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(true));
		var logs = new Mock<IAuditLogRepository>();
		var unit = new Mock<IUnitOfWork>();
		unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(1));
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddApplication();
		services.AddSingleton(repository.Object);
		services.AddSingleton(access.Object);
		services.AddSingleton(snapshots.Object);
		services.AddSingleton(logs.Object);
		services.AddSingleton(unit.Object);
		services.AddSingleton(Mock.Of<ICurrentUserService>());
		await using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();
		var handler = new ImportRedisDataJobHandler(repository.Object, access.Object, scope.ServiceProvider.GetRequiredService<RedisGUI.Shared.Mediator.IMediator>());
		var data = JsonSerializer.Serialize(new[] { new RedisImportItem { Key = "a", Value = "1" }, new RedisImportItem { Key = "b", Value = "2" } });
		var parameters = JsonSerializer.SerializeToElement(new { data, format = "json", database = 0, overwrite = false });
		using var cancellation = new CancellationTokenSource();
		var result = await handler.ExecuteAsync(connection.Id, parameters, (_, _) => { cancellation.Cancel(); return Task.CompletedTask; }, cancellation.Token);
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value, Is.InstanceOf<ImportRedisDataResponse>());
		var response = (ImportRedisDataResponse)result.Value!;
		Assert.That(response.ImportedCount, Is.EqualTo(1));
		Assert.That(response.Cancelled, Is.True);
		logs.Verify(x => x.Add(It.Is<AuditLog>(log => log.Metadata.NewValue!.Contains("cancelled: True"))), Times.Once);
	}
}
