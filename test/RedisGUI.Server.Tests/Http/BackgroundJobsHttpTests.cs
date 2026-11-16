using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RedisGUI.Application;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Infrastructure.SignalR.Hubs;
using RedisGUI.Server.Endpoints.Jobs;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Server.Tests.Http;

[TestFixture]
public class BackgroundJobsHttpTests
{
	[Test]
	public async Task Jobs_AreIndependentOfRequestsAndScopedToTheirOwner()
	{
		var owner = Guid.NewGuid();
		var stranger = Guid.NewGuid();
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var allowProgress = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var loadTests = new Mock<IRedisLoadTestService>();
		loadTests.Setup(x => x.RunStreamingLoadTestAsync(connection, It.IsAny<RedisLoadTestRequest>(),
			It.IsAny<Func<RedisLoadTestProgress, Task>>(), It.IsAny<CancellationToken>()))
			.Returns(async (RedisConnection _, RedisLoadTestRequest _, Func<RedisLoadTestProgress, Task> progress, CancellationToken token) =>
			{
				await allowProgress.Task.WaitAsync(token);
				await progress(new RedisLoadTestProgress { CompletedOperations = 7, TotalOperations = 100 });
				entered.TrySetResult();
				await Task.Delay(Timeout.Infinite, token);
				return Result.Success(new RedisLoadTestResult());
			});
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddApplication();
		builder.Services.AddSingleton<IBackgroundJobHandler>(new TestFileJobHandler());
		builder.Services.AddSingleton<IBackgroundJobHandler>(new TestFailureJobHandler());
		builder.Services.AddProblemDetails();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddSignalR();
		builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("sensitive", limiter =>
		{
			limiter.PermitLimit = 100;
			limiter.Window = TimeSpan.FromMinutes(1);
		}));
		builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
		builder.Services.AddAuthorization(options =>
		{
			options.AddPolicy(Policies.Authenticated, policy => policy.RequireAuthenticatedUser());
			options.AddPolicy(Policies.WriteAccess, policy => policy.RequireRole("Admin", "User"));
		});
		builder.Services.AddScoped<IAuthorizationHandler, ConnectionOwnershipHandler>();
		builder.Services.AddSingleton(repository.Object);
		builder.Services.AddSingleton(access.Object);
		builder.Services.AddSingleton(loadTests.Object);
		builder.Services.AddSingleton(Mock.Of<IAuditLogRepository>());
		builder.Services.AddSingleton(Mock.Of<IUnitOfWork>());
		builder.Services.AddScoped<ICurrentUserService, TestCurrentUser>();
		builder.Services.AddScoped(typeof(BackgroundJobManager).Assembly.GetType("RedisGUI.Infrastructure.BackgroundJobs.JobIdentity")!);
		builder.Services.AddSingleton<BackgroundJobManager>();
		builder.Services.AddHostedService(provider => provider.GetRequiredService<BackgroundJobManager>());
		await using var app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseRateLimiter();
		new BackgroundJobsEndpoint().MapEndpoint(app);
		app.MapHub<BackgroundJobsHub>("/hub/jobs");
		await app.StartAsync();
		var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
		using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
		var body = new { kind = "load-test", connectionId = connection.Id, parameters = new { operationsCount = 100 } };
		using var anonymous = await client.PostAsJsonAsync("/jobs", body);
		Assert.That(anonymous.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
		using var readOnlyRequest = Request(HttpMethod.Post, "/jobs", owner, "ReadOnly");
		readOnlyRequest.Content = JsonContent.Create(body);
		using var denied = await client.SendAsync(readOnlyRequest);
		Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
		using var start = Request(HttpMethod.Post, "/jobs", owner);
		start.Content = JsonContent.Create(body);
		using var started = await client.SendAsync(start);
		Assert.That(started.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		var initial = await started.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
		Assert.That(initial, Is.Not.Null);
		await using var ownerHub = new HubConnectionBuilder().WithUrl(address + "/hub/jobs", options =>
		{
			options.Headers.Add("X-Test-User", owner.ToString());
			options.Headers.Add("X-Test-Role", "User");
		}).Build();
		await using var otherHub = new HubConnectionBuilder().WithUrl(address + "/hub/jobs", options =>
		{
			options.Headers.Add("X-Test-User", stranger.ToString());
			options.Headers.Add("X-Test-Role", "User");
		}).Build();
		var progressEvent = new TaskCompletionSource<BackgroundJobSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
		var completionEvent = new TaskCompletionSource<BackgroundJobSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
		ownerHub.On<BackgroundJobSnapshot>("JobUpdated", job => { if (job.ProgressPercent.HasValue) progressEvent.TrySetResult(job); });
		ownerHub.On<BackgroundJobSnapshot>("JobCompleted", job => completionEvent.TrySetResult(job));
		await ownerHub.StartAsync();
		await otherHub.StartAsync();
		await ownerHub.InvokeAsync("Subscribe", initial!.JobId);
		await Assert.ThrowsAsync<HubException>(async () => await otherHub.InvokeAsync("Subscribe", initial.JobId));
		allowProgress.TrySetResult();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That((await progressEvent.Task.WaitAsync(TimeSpan.FromSeconds(5))).ProgressPercent, Is.EqualTo(7).Within(0.001));
		await ownerHub.StopAsync();
		await ownerHub.StartAsync();
		await ownerHub.InvokeAsync("Subscribe", initial.JobId);
		using var other = await client.SendAsync(Request(HttpMethod.Get, $"/jobs/{initial!.JobId}", stranger));
		Assert.That(other.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		using var otherCancel = await client.SendAsync(Request(HttpMethod.Post, $"/jobs/{initial.JobId}/cancel", stranger));
		Assert.That(otherCancel.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		using var status = await client.SendAsync(Request(HttpMethod.Get, $"/jobs/{initial.JobId}", owner));
		var progressSnapshot = await status.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
		Assert.That(progressSnapshot!.ProgressPercent, Is.EqualTo(7).Within(0.001));
		using var cancel = await client.SendAsync(Request(HttpMethod.Post, $"/jobs/{initial.JobId}/cancel", owner));
		Assert.That(cancel.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
		var manager = app.Services.GetRequiredService<BackgroundJobManager>();
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		while (manager.Get(initial.JobId, owner)?.Status != "Cancelled") await Task.Delay(10, deadline.Token);
		Assert.That(manager.Get(initial.JobId, owner)!.Status, Is.EqualTo("Cancelled"));
		Assert.That((await completionEvent.Task.WaitAsync(TimeSpan.FromSeconds(5))).Status, Is.EqualTo("Cancelled"));

		loadTests.Setup(x => x.RunStreamingLoadTestAsync(connection, It.IsAny<RedisLoadTestRequest>(),
			It.IsAny<Func<RedisLoadTestProgress, Task>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(new RedisLoadTestResult { SuccessfulOperations = 5 }));
		using var second = Request(HttpMethod.Post, "/jobs", owner);
		second.Content = JsonContent.Create(body);
		using var completedStart = await client.SendAsync(second);
		var completedJob = await completedStart.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
		while (manager.Get(completedJob!.JobId, owner)?.Status != "Completed") await Task.Delay(10, deadline.Token);
		using var unauthorizedResult = await client.SendAsync(Request(HttpMethod.Get, $"/jobs/{completedJob.JobId}/result", stranger));
		Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		using var result = await client.SendAsync(Request(HttpMethod.Get, $"/jobs/{completedJob.JobId}/result", owner));
		Assert.That((await result.Content.ReadFromJsonAsync<RedisLoadTestResult>())!.SuccessfulOperations, Is.EqualTo(5));

		using var fileStart = Request(HttpMethod.Post, "/jobs", owner);
		fileStart.Content = JsonContent.Create(new { kind = "test-file", connectionId = connection.Id, parameters = new { } });
		using var fileStarted = await client.SendAsync(fileStart);
		var fileJob = await fileStarted.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
		while (manager.Get(fileJob!.JobId, owner)?.Status != "Completed") await Task.Delay(10, deadline.Token);
		using var fileResponse = await client.SendAsync(Request(HttpMethod.Get, $"/jobs/{fileJob.JobId}/result", owner));
		Assert.That(fileResponse.Content.Headers.ContentType!.MediaType, Is.EqualTo("text/plain"));
		Assert.That(await fileResponse.Content.ReadAsStringAsync(), Is.EqualTo("large-result"));

		using var failureStart = Request(HttpMethod.Post, "/jobs", owner);
		failureStart.Content = JsonContent.Create(new { kind = "test-failure", connectionId = connection.Id, parameters = new { } });
		using var failureStarted = await client.SendAsync(failureStart);
		var failedJob = await failureStarted.Content.ReadFromJsonAsync<BackgroundJobSnapshot>();
		while (manager.Get(failedJob!.JobId, owner)?.Status != "Failed") await Task.Delay(10, deadline.Token);
		Assert.That(manager.Get(failedJob.JobId, owner)!.Error, Is.EqualTo("Expected failure."));
		using var partialResult = await client.SendAsync(Request(HttpMethod.Get, $"/jobs/{failedJob.JobId}/result", owner));
		Assert.That(partialResult.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(await partialResult.Content.ReadAsStringAsync(), Does.Contain("\"processed\":2"));
		await ownerHub.StopAsync();
		await otherHub.StopAsync();
		await app.StopAsync();
	}

	private static HttpRequestMessage Request(HttpMethod method, string path, Guid user, string role = "User")
	{
		var request = new HttpRequestMessage(method, path);
		request.Headers.Add("X-Test-User", user.ToString());
		request.Headers.Add("X-Test-Role", role);
		return request;
	}

	private sealed class TestCurrentUser(IHttpContextAccessor context) : ICurrentUserService
	{
		public Guid? UserId => Guid.TryParse(context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
		public string? IpAddress => null;
		public string? UserAgent => null;
		public bool IsAuthenticated => UserId.HasValue;
	}

	private sealed class TestFileJobHandler : IBackgroundJobHandler
	{
		public string Kind => "test-file";
		public bool RequiresWriteAccess => false;
		public Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken token) => Task.FromResult(Result.Success());
		public async Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters, Func<double?, object?, Task> progress, CancellationToken token)
		{
			var path = Path.Combine(Path.GetTempPath(), "redisgui-job-test-" + Guid.NewGuid().ToString("N") + ".txt");
			await File.WriteAllTextAsync(path, "large-result", token);
			return Result.Success<object?>(new BackgroundJobFile(path, "result.txt", "text/plain"));
		}
	}

	private sealed class TestFailureJobHandler : IBackgroundJobHandler
	{
		public string Kind => "test-failure";
		public bool RequiresWriteAccess => false;
		public Task<Result> ValidateAsync(Guid connectionId, JsonElement parameters, CancellationToken token) => Task.FromResult(Result.Success());
		public Task<Result<object?>> ExecuteAsync(Guid connectionId, JsonElement parameters, Func<double?, object?, Task> progress, CancellationToken token) =>
			Task.FromResult(Result.Success<object?>(new BackgroundJobOutcome(new { processed = 2 }, "Expected failure.")));
	}

	private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
		: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (!Guid.TryParse(Request.Headers["X-Test-User"], out var id)) return Task.FromResult(AuthenticateResult.NoResult());
			var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, Request.Headers["X-Test-Role"].ToString())], Scheme.Name);
			return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
		}
	}
}
