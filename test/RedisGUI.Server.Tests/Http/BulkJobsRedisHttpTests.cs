using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
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
using RedisGUI.Application.Redis.Commands.BulkOperations;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Server.Endpoints.Jobs;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Server.Tests.Http;

[TestFixture]
public class BulkJobsRedisHttpTests
{
	[Test]
	public async Task PatternJob_DeletesAllMatchingPagesAndReturnsAuditedResult()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var mux = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Bulk job test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<RedisConnection>(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		var owner = Guid.NewGuid();
		var currentUser = new Mock<ICurrentUserService>();
		currentUser.SetupGet(x => x.UserId).Returns(owner);
		var logs = new Mock<IAuditLogRepository>();
		var unit = new Mock<IUnitOfWork>();
		unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(1));
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddApplication();
		builder.Services.AddProblemDetails();
		builder.Services.AddSignalR();
		builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuth>("Test", _ => { });
		builder.Services.AddAuthorization(options =>
		{
			options.AddPolicy(Policies.Authenticated, policy => policy.RequireAuthenticatedUser());
			options.AddPolicy(Policies.WriteAccess, policy => policy.RequireRole("User"));
		});
		builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("sensitive", limiter =>
		{
			limiter.PermitLimit = 20;
			limiter.Window = TimeSpan.FromMinutes(1);
		}));
		builder.Services.AddSingleton(manager.Object);
		builder.Services.AddSingleton(repository.Object);
		builder.Services.AddSingleton(access.Object);
		builder.Services.AddSingleton(logs.Object);
		builder.Services.AddSingleton(unit.Object);
		builder.Services.AddSingleton(currentUser.Object);
		builder.Services.AddScoped<JobIdentity>();
		builder.Services.AddScoped<IRedisKeyService, RedisKeyService>();
		builder.Services.AddSingleton<BackgroundJobManager>();
		builder.Services.AddHostedService(provider => provider.GetRequiredService<BackgroundJobManager>());
		await using var app = builder.Build();
		app.UseRouting();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseRateLimiter();
		new BackgroundJobsEndpoint().MapEndpoint(app);
		await app.StartAsync();
		var address = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
		using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };
		var prefix = "redisgui-bulk-job:" + Guid.NewGuid().ToString("N") + ":";
		var names = Enumerable.Range(0, 150).Select(i => prefix + i).ToArray();
		var source = mux.GetDatabase(1);
		var other = mux.GetDatabase(2);
		try
		{
			foreach (var key in names) await source.StringSetAsync(key, "value");
			await other.StringSetAsync(names[0], "keep");
			using var start = await client.PostAsJsonAsync("/jobs", new { kind = "bulk-keys", connectionId = connection.Id,
				parameters = new { operation = "delete-pattern", pattern = prefix + "*", database = 1 } });
			Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await start.Content.ReadAsStringAsync());
			var job = await start.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
			var jobs = app.Services.GetRequiredService<BackgroundJobManager>();
			using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
			while (jobs.Get(job!.JobId, owner)?.Status is "Queued" or "Running") await Task.Delay(10, deadline.Token);
			Assert.That(jobs.Get(job!.JobId, owner)!.Status, Is.EqualTo("Completed"));
			using var response = await client.GetAsync($"/jobs/{job.JobId}/result");
			var result = await response.Content.ReadFromJsonAsync<BulkKeyJobResult>();
			Assert.That(result!.Changed, Is.EqualTo(150));
			Assert.That(await other.KeyExistsAsync(names[0]), Is.True);
			logs.Verify(x => x.Add(It.Is<AuditLog>(log => log.Metadata.NewValue!.Contains("Deleted 150 matching keys"))), Times.Once);
		}
		finally
		{
			foreach (var key in names) await source.KeyDeleteAsync(key);
			await other.KeyDeleteAsync(names[0]);
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
