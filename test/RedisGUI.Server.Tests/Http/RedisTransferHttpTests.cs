using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RedisGUI.Application;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Server.Endpoints.Redis;
using RedisGUI.Server.Endpoints.Jobs;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Server.Tests.Http;

[TestFixture]
public class RedisTransferHttpTests
{
	[Test]
	public async Task ExportAndImport_PreserveAllPagesBinaryKeysAndStreams()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var mux = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Transfer test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok<RedisConnection>(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		var owner = Guid.NewGuid();
		var currentUser = new Mock<ICurrentUserService>();
		currentUser.SetupGet(x => x.UserId).Returns(owner);
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddApplication();
		builder.Services.AddProblemDetails();
		builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuth>("Test", _ => { });
		builder.Services.AddAuthorization(options =>
		{
			options.AddPolicy(Policies.Authenticated, policy => policy.RequireAuthenticatedUser());
			options.AddPolicy(Policies.WriteAccess, policy => policy.RequireRole("User"));
		});
		builder.Services.AddSignalR();
		builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("sensitive", policy =>
		{
			policy.PermitLimit = 50;
			policy.Window = TimeSpan.FromMinutes(1);
			policy.QueueLimit = 0;
		}));
		builder.Services.AddSingleton(manager.Object);
		builder.Services.AddSingleton(repository.Object);
		builder.Services.AddSingleton(access.Object);
		builder.Services.AddSingleton(Mock.Of<IAuditLogRepository>());
		builder.Services.AddSingleton(Mock.Of<IUnitOfWork>());
		builder.Services.AddSingleton(currentUser.Object);
		builder.Services.AddScoped<JobIdentity>();
		builder.Services.AddSingleton<BackgroundJobManager>();
		builder.Services.AddHostedService(provider => provider.GetRequiredService<BackgroundJobManager>());
		builder.Services.AddScoped<IRedisKeyService, RedisKeyService>();
		builder.Services.AddScoped<IRedisSnapshotService, RedisSnapshotService>();
		builder.Services.AddScoped<IExportSessionFactory, RedisExportSessionFactory>();
		await using var app = builder.Build();
		app.UseRouting();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseRateLimiter();
		new BackgroundJobsEndpoint().MapEndpoint(app);
		await app.StartAsync();
		var address = System.Linq.Enumerable.Single(app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>().Features.Get<IServerAddressesFeature>()!.Addresses);
		using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
		var prefix = "redisgui-transfer-test:" + Guid.NewGuid().ToString("N") + ":";
		var allKeys = new List<byte[]>();
		for (int i = 0; i < 120; i++) allKeys.Add(Encoding.UTF8.GetBytes(prefix + i));
		byte[] binaryKey = [.. Encoding.UTF8.GetBytes(prefix), 0xff, 0x01];
		byte[] streamKey = Encoding.UTF8.GetBytes(prefix + "stream");
		allKeys.Add(binaryKey);
		allKeys.Add(streamKey);
		var source = mux.GetDatabase(1);
		var destination = mux.GetDatabase(2);
		try
		{
			foreach (var key in allKeys.Take(120)) await source.StringSetAsync((RedisKey)key, "value", TimeSpan.FromMinutes(5));
			await source.StringSetAsync((RedisKey)binaryKey, (RedisValue)new byte[] { 0, 0xff, 0x01 });
			for (int i = 0; i < 120; i++) await source.StreamAddAsync((RedisKey)streamKey, "item", i);
			using var start = await client.PostAsJsonAsync("/jobs", new { kind = "redis-export", connectionId = connection.Id, parameters = new { format = "json", database = 1, pattern = prefix + "*" } });
			Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await start.Content.ReadAsStringAsync());
			var job = await start.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
			using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
			var jobs = app.Services.GetRequiredService<BackgroundJobManager>();
			while (jobs.Get(job!.JobId, owner)?.Status is "Queued" or "Running") await Task.Delay(10, deadline.Token);
			Assert.That(jobs.Get(job!.JobId, owner)!.Status, Is.EqualTo("Completed"));
			using var export = await client.GetAsync($"/jobs/{job.JobId}/result");
			Assert.That(export.StatusCode, Is.EqualTo(HttpStatusCode.OK), await export.Content.ReadAsStringAsync());
			var data = await export.Content.ReadAsStringAsync();
			using (var json = JsonDocument.Parse(data)) Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(allKeys.Count));

			async Task<HttpResponseMessage> ImportAsync()
			{
				using var importStart = await client.PostAsJsonAsync("/jobs", new { kind = "redis-import", connectionId = connection.Id, parameters = new { data, format = "json", database = 2, overwrite = false } });
				Assert.That(importStart.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await importStart.Content.ReadAsStringAsync());
				var importJob = await importStart.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
				while (jobs.Get(importJob!.JobId, owner)?.Status is "Queued" or "Running") await Task.Delay(10, deadline.Token);
				Assert.That(jobs.Get(importJob!.JobId, owner)!.Status, Is.EqualTo("Completed"));
				return await client.GetAsync($"/jobs/{importJob.JobId}/result");
			}
			using var import = await ImportAsync();
			Assert.That(import.StatusCode, Is.EqualTo(HttpStatusCode.OK), await import.Content.ReadAsStringAsync());
			using (var json = JsonDocument.Parse(await import.Content.ReadAsStringAsync()))
			{
				Assert.That(json.RootElement.GetProperty("importedCount").GetInt32(), Is.EqualTo(allKeys.Count));
				Assert.That(json.RootElement.GetProperty("failedCount").GetInt32(), Is.Zero);
			}
			Assert.That((byte[])(await destination.StringGetAsync((RedisKey)binaryKey))!, Is.EqualTo(new byte[] { 0, 0xff, 0x01 }));
			Assert.That(await destination.StreamLengthAsync((RedisKey)streamKey), Is.EqualTo(120));
			Assert.That(await destination.KeyTimeToLiveAsync((RedisKey)allKeys[0]), Is.Not.Null);

			using var again = await ImportAsync();
			using var repeat = JsonDocument.Parse(await again.Content.ReadAsStringAsync());
			Assert.That(repeat.RootElement.GetProperty("skippedCount").GetInt32(), Is.EqualTo(allKeys.Count));
		}
		finally
		{
			foreach (var key in allKeys)
			{
				await source.KeyDeleteAsync((RedisKey)key);
				await destination.KeyDeleteAsync((RedisKey)key);
			}
			await app.StopAsync();
		}
	}

	private sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
		: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "User")], Scheme.Name);
			return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
		}
	}
}
