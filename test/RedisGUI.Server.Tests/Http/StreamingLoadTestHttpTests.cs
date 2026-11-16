using System;
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
using Microsoft.AspNetCore.Http;
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
using RedisGUI.Server.Endpoints.RedisStorage.RunLoadTest;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Server.Tests.Http;

[TestFixture]
public class StreamingLoadTestHttpTests
{
	[Test]
	public async Task Stream_RequiresAuthenticationAndOnlyOwnerCanStopIt()
	{
		var owner = Guid.NewGuid();
		var stranger = Guid.NewGuid();
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var loadTests = new Mock<IRedisLoadTestService>();
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(It.IsAny<RedisConnection>())).ReturnsAsync(true);
		loadTests.Setup(x => x.RunStreamingLoadTestAsync(connection, It.IsAny<RedisLoadTestRequest>(),
			It.IsAny<Func<RedisLoadTestProgress, Task>>(), It.IsAny<CancellationToken>()))
			.Returns(async (RedisConnection _, RedisLoadTestRequest _, Func<RedisLoadTestProgress, Task> progress, CancellationToken token) =>
			{
				await progress(new RedisLoadTestProgress { CompletedOperations = 7 });
				await Task.Delay(Timeout.Infinite, token);
				return Result.Success(new RedisLoadTestResult());
			});

		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddApplication();
		builder.Services.AddProblemDetails();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(Policies.WriteAccess, policy => policy.RequireRole("Admin", "User")));
		builder.Services.AddSingleton(repository.Object);
		builder.Services.AddSingleton(loadTests.Object);
		builder.Services.AddSingleton(access.Object);
		builder.Services.AddSingleton(Mock.Of<IAuditLogRepository>());
		builder.Services.AddSingleton(Mock.Of<IUnitOfWork>());
		builder.Services.AddScoped<ICurrentUserService, TestCurrentUser>();
		await using var app = builder.Build();
		app.UseAuthentication();
		app.UseAuthorization();
		new RunLoadTestStreamEndpoint().MapEndpoint(app);
		await app.StartAsync();
		var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
		using var client = new HttpClient { BaseAddress = new Uri(System.Linq.Enumerable.Single(address)), Timeout = TimeSpan.FromSeconds(10) };
		var path = $"/storage/{connection.Id}/load-test";
		var unauthenticated = await client.PostAsJsonAsync(path + "/stream?testId=anonymous", new { });
		Assert.That(unauthenticated.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
		using var readOnlyUser = new HttpRequestMessage(HttpMethod.Post, path + "/stream?testId=reader") { Content = JsonContent.Create(new { }) };
		readOnlyUser.Headers.Add("X-Test-User", owner.ToString());
		readOnlyUser.Headers.Add("X-Test-Role", "ReadOnly");
		using var roleResponse = await client.SendAsync(readOnlyUser);
		Assert.That(roleResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

		using var request = new HttpRequestMessage(HttpMethod.Post, path + "/stream?testId=owned") { Content = JsonContent.Create(new { }) };
		request.Headers.Add("X-Test-User", owner.ToString());
		using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
		using var reader = new System.IO.StreamReader(await response.Content.ReadAsStreamAsync());
		Assert.That(await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("event: progress"));
		Assert.That(await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)), Does.Contain("\"completedOperations\":7"));

		using var forbiddenStop = new HttpRequestMessage(HttpMethod.Post, path + "/stop") { Content = JsonContent.Create(new { testId = "owned" }) };
		forbiddenStop.Headers.Add("X-Test-User", stranger.ToString());
		using var otherResponse = await client.SendAsync(forbiddenStop);
		Assert.That(await otherResponse.Content.ReadAsStringAsync(), Does.Contain("\"success\":false"));

		using var stop = new HttpRequestMessage(HttpMethod.Post, path + "/stop") { Content = JsonContent.Create(new { testId = "owned" }) };
		stop.Headers.Add("X-Test-User", owner.ToString());
		using var stopResponse = await client.SendAsync(stop);
		Assert.That(await stopResponse.Content.ReadAsStringAsync(), Does.Contain("\"success\":true"));
		await reader.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));

		access.Setup(x => x.CanAccessAsync(It.IsAny<RedisConnection>())).ReturnsAsync(false);
		using var denied = new HttpRequestMessage(HttpMethod.Post, path + "/stream?testId=denied") { Content = JsonContent.Create(new { }) };
		denied.Headers.Add("X-Test-User", owner.ToString());
		using var deniedResponse = await client.SendAsync(denied);
		Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
		Assert.That(deniedResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

		access.Setup(x => x.CanAccessAsync(It.IsAny<RedisConnection>())).ReturnsAsync(true);
		connection.SetReadOnly(true);
		using var readOnly = new HttpRequestMessage(HttpMethod.Post, path + "/stream?testId=readonly") { Content = JsonContent.Create(new { }) };
		readOnly.Headers.Add("X-Test-User", owner.ToString());
		using var readOnlyResponse = await client.SendAsync(readOnly);
		Assert.That(readOnlyResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
		connection.SetReadOnly(false);

		loadTests.Setup(x => x.RunStreamingLoadTestAsync(connection, It.IsAny<RedisLoadTestRequest>(),
			It.IsAny<Func<RedisLoadTestProgress, Task>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(new RedisLoadTestResult { SuccessfulOperations = 5 }));
		using var complete = new HttpRequestMessage(HttpMethod.Post, path + "/stream?testId=complete") { Content = JsonContent.Create(new { }) };
		complete.Headers.Add("X-Test-User", owner.ToString());
		using var completeResponse = await client.SendAsync(complete);
		var completeBody = await completeResponse.Content.ReadAsStringAsync();
		Assert.That(completeBody, Does.Contain("event: complete"));
		Assert.That(completeBody, Does.Contain("\"successfulOperations\":5"));
		await app.StopAsync();
	}

	private sealed class TestCurrentUser(IHttpContextAccessor context) : ICurrentUserService
	{
		public Guid? UserId => Guid.TryParse(context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
		public string? IpAddress => null;
		public string? UserAgent => null;
		public bool IsAuthenticated => UserId.HasValue;
	}

	private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
		: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (!Guid.TryParse(Request.Headers["X-Test-User"], out var userId)) return Task.FromResult(AuthenticateResult.NoResult());
			var role = Request.Headers["X-Test-Role"].ToString();
			var identity = new ClaimsIdentity([
				new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
				new Claim(ClaimTypes.Role, string.IsNullOrEmpty(role) ? "User" : role)
			], Scheme.Name);
			return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
		}
	}
}
