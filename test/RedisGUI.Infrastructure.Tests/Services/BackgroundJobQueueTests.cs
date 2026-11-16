using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.Tests.Services;

[TestFixture]
public class BackgroundJobQueueTests
{
	[Test]
	public void Queue_RejectsPayloadsThatExceedSharedMemoryBudget()
	{
		using var manager = new BackgroundJobManager(Mock.Of<IServiceScopeFactory>(), Mock.Of<IHubContext<BackgroundJobsHub>>(), NullLogger<BackgroundJobManager>.Instance);
		var owner = Guid.NewGuid();
		var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.ToString())], "test"));
		using var parameters = JsonDocument.Parse("{\"data\":\"" + new string('a', 17 * 1024 * 1024) + "\"}");
		Assert.That(manager.TryStart(owner, principal, Guid.NewGuid(), "test", parameters.RootElement, out _), Is.True);
		Assert.That(manager.TryStart(owner, principal, Guid.NewGuid(), "test", parameters.RootElement, out _), Is.False);
	}

	[Test]
	public async Task Queue_IsBoundedAndOnlyOwnerCanCancel()
	{
		var proxy = new Mock<IClientProxy>();
		proxy.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
		var clients = new Mock<IHubClients>();
		clients.Setup(x => x.Group(It.IsAny<string>())).Returns(proxy.Object);
		var hub = new Mock<IHubContext<BackgroundJobsHub>>();
		hub.SetupGet(x => x.Clients).Returns(clients.Object);
		using var manager = new BackgroundJobManager(Mock.Of<IServiceScopeFactory>(), hub.Object, NullLogger<BackgroundJobManager>.Instance);
		var owner = Guid.NewGuid();
		var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.ToString())], "test"));
		using var parameters = JsonDocument.Parse("{}");
		BackgroundJobSnapshot? first = null;
		for (int i = 0; i < 100; i++)
		{
			Assert.That(manager.TryStart(owner, principal, Guid.NewGuid(), "test", parameters.RootElement, out var job), Is.True);
			first ??= job;
		}
		Assert.That(manager.TryStart(owner, principal, Guid.NewGuid(), "test", parameters.RootElement, out _), Is.False);
		Assert.That(manager.Get(first!.JobId, Guid.NewGuid()), Is.Null);
		Assert.That(await manager.CancelAsync(first.JobId, Guid.NewGuid()), Is.False);
		Assert.That(await manager.CancelAsync(first.JobId, owner), Is.True);
		Assert.That(manager.Get(first.JobId, owner)!.Status, Is.EqualTo("Cancelling"));
	}
}
