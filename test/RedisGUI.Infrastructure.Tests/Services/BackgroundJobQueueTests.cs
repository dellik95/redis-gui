using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Infrastructure.SignalR.Hubs;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Infrastructure.Tests.Services;

[TestFixture]
public class BackgroundJobQueueTests
{
	private sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
	{
		private DateTimeOffset now = start;
		public override DateTimeOffset GetUtcNow() => now;
		public void Advance(TimeSpan delta) => now = now.Add(delta);
	}

	private sealed class CancellationFailureRaceHandler(TaskCompletionSource<bool> cancelledObserved) : IBackgroundJobHandler
	{
		public string Kind => "race-fail";
		public bool RequiresWriteAccess => false;
		public Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken) =>
			Task.FromResult(Result.Success());
		public async Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters, Func<double?, object?, Task> progress, CancellationToken cancellationToken)
		{
			await progress(50, new { partial = 3 });
			try { await Task.Delay(Timeout.Infinite, cancellationToken); }
			catch (OperationCanceledException) { }
			cancelledObserved.TrySetResult(cancellationToken.IsCancellationRequested);
			return Result.Success<object?>(new BackgroundJobOutcome(new { partial = 3 }, "Write failed during stop"));
		}
	}

	private sealed class TempFileResultHandler : IBackgroundJobHandler
	{
		public string Kind => "temp-file";
		public bool RequiresWriteAccess => false;
		public string? CreatedPath { get; private set; }
		public Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken cancellationToken) =>
			Task.FromResult(Result.Success());
		public async Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters, Func<double?, object?, Task> progress, CancellationToken cancellationToken)
		{
			Directory.CreateDirectory(BackgroundJobTempStorage.DirectoryPath);
			var path = Path.Combine(BackgroundJobTempStorage.DirectoryPath, $"redisgui-job-lease-{Guid.NewGuid():N}.txt");
			await File.WriteAllTextAsync(path, "download-lease-content", cancellationToken);
			CreatedPath = path;
			return Result.Success<object?>(new BackgroundJobFile(path, "lease.txt", "text/plain"));
		}
	}

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

	[Test]
	public async Task ConcurrentStartsListsAndCancels_EnforceCapacityWithoutLocks()
	{
		var options = Options.Create(new BackgroundJobOptions { MaxRetainedJobs = 25, QueueCapacity = 25, WorkerCount = 1 });
		var hub = CreateNoopHub();
		using var manager = new BackgroundJobManager(Mock.Of<IServiceScopeFactory>(), hub.Object, NullLogger<BackgroundJobManager>.Instance, options);
		var owner = Guid.NewGuid();
		var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.ToString())], "test"));
		using var doc = JsonDocument.Parse("{\"v\":1}");
		var payload = doc.RootElement.Clone();

		int admitted = 0;
		var workers = Enumerable.Range(0, 16).Select(workerIndex => Task.Run(async () =>
		{
			for (int i = 0; i < 10; i++)
			{
				if (manager.TryStart(owner, principal, Guid.NewGuid(), "stress", payload, out var started) && started is not null)
				{
					Interlocked.Increment(ref admitted);
					_ = manager.ListForOwner(owner);
					_ = manager.Get(started.JobId, owner);
					await manager.CancelAsync(started.JobId, owner);
				}
			}
		}));

		await Task.WhenAll(workers);
		Assert.That(admitted, Is.LessThanOrEqualTo(25));
		Assert.That(manager.ListForOwner(owner).Count, Is.EqualTo(admitted));
	}

	[Test]
	public async Task ExecutionFailure_AfterCancellationRequested_EndsAsFailedWithPartialResult()
	{
		var cancelledObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var services = new ServiceCollection();
		services.AddSingleton<IBackgroundJobHandler>(new CancellationFailureRaceHandler(cancelledObserved));
		services.AddScoped<JobIdentity>();
		await using var provider = services.BuildServiceProvider();

		var hub = CreateNoopHub();
		using var manager = new BackgroundJobManager(
			provider.GetRequiredService<IServiceScopeFactory>(),
			hub.Object,
			NullLogger<BackgroundJobManager>.Instance);

		await manager.StartAsync(CancellationToken.None);
		try
		{
			var owner = Guid.NewGuid();
			var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.ToString())], "test"));
			using var doc = JsonDocument.Parse("{}");
			Assert.That(manager.TryStart(owner, principal, Guid.NewGuid(), "race-fail", doc.RootElement, out var started), Is.True);

			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			while (manager.Get(started!.JobId, owner)?.Status is "Queued")
			{
				await Task.Delay(10, timeout.Token);
			}

			Assert.That(await manager.CancelAsync(started.JobId, owner), Is.True);
			Assert.That(await cancelledObserved.Task.WaitAsync(timeout.Token), Is.True);

			BackgroundJobSnapshot? terminal;
			while ((terminal = manager.Get(started.JobId, owner))?.Status is "Running" or "Cancelling")
			{
				await Task.Delay(10, timeout.Token);
			}

			Assert.That(terminal!.Status, Is.EqualTo("Failed"));
			Assert.That(terminal.CancellationRequested, Is.True);
			Assert.That(terminal.Error, Is.EqualTo("Write failed during stop"));
			Assert.That(terminal.HasResult, Is.True);
			Assert.That(manager.GetResult(started.JobId, owner), Is.Not.Null);
		}
		finally
		{
			await manager.StopAsync(CancellationToken.None);
		}
	}

	[Test]
	public async Task ExpiredResultFile_WaitsForActiveDownloadLeaseBeforeDeleting()
	{
		var time = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
		var handler = new TempFileResultHandler();
		var services = new ServiceCollection();
		services.AddSingleton<IBackgroundJobHandler>(handler);
		services.AddScoped<JobIdentity>();
		await using var provider = services.BuildServiceProvider();

		var options = Options.Create(new BackgroundJobOptions
		{
			ResultRetention = TimeSpan.FromSeconds(5),
			CleanupInterval = TimeSpan.FromHours(1)
		});
		var hub = CreateNoopHub();
		using var manager = new BackgroundJobManager(
			provider.GetRequiredService<IServiceScopeFactory>(),
			hub.Object,
			NullLogger<BackgroundJobManager>.Instance,
			options,
			time);

		await manager.StartAsync(CancellationToken.None);
		try
		{
			var owner = Guid.NewGuid();
			var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner.ToString())], "test"));
			using var doc = JsonDocument.Parse("{}");
			Assert.That(manager.TryStart(owner, principal, Guid.NewGuid(), "temp-file", doc.RootElement, out var started), Is.True);

			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			while (manager.Get(started!.JobId, owner)?.Status != "Completed")
			{
				await Task.Delay(10, timeout.Token);
			}

			Assert.That(handler.CreatedPath, Is.Not.Null);
			Assert.That(File.Exists(handler.CreatedPath), Is.True);
			Assert.That(manager.TryPinResultFile(started.JobId, owner, out var file, out var lease), Is.True);
			Assert.That(file!.Path, Is.EqualTo(handler.CreatedPath));

			time.Advance(TimeSpan.FromSeconds(30));
			manager.RemoveExpired();
			Assert.That(File.Exists(handler.CreatedPath), Is.True, "Active download lease must prevent premature file deletion.");
			Assert.That(manager.Get(started.JobId, owner), Is.Not.Null);

			lease!.Dispose();
			manager.RemoveExpired();
			Assert.That(File.Exists(handler.CreatedPath), Is.False, "Expired file should be deleted once all download leases finish.");
			Assert.That(manager.Get(started.JobId, owner), Is.Null);
		}
		finally
		{
			if (handler.CreatedPath is not null && File.Exists(handler.CreatedPath))
			{
				File.Delete(handler.CreatedPath);
			}
			await manager.StopAsync(CancellationToken.None);
		}
	}

	private static Mock<IHubContext<BackgroundJobsHub>> CreateNoopHub()
	{
		var proxy = new Mock<ISingleClientProxy>();
		proxy.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		var clients = new Mock<IHubClients>();
		clients.Setup(x => x.Group(It.IsAny<string>())).Returns(proxy.Object);
		clients.Setup(x => x.Client(It.IsAny<string>())).Returns(proxy.Object);
		var hub = new Mock<IHubContext<BackgroundJobsHub>>();
		hub.SetupGet(x => x.Clients).Returns(clients.Object);
		return hub;
	}
}
