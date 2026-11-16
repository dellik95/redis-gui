using System;
using System.IO;
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
using RedisGUI.Server.Endpoints.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Server.Tests.Http;

[TestFixture]
public class BulkPatternStreamHttpTests
{
	[Test]
	public async Task Stop_ReturnsConfirmedCountAndAuditsCancelledOperation()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		var keys = new Mock<IRedisKeyService>();
		keys.Setup(x => x.DeleteKeysByPatternAsync(connection, "test:*", It.IsAny<CancellationToken>(), 1, It.IsAny<Func<int, Task>>()))
			.Returns(async (RedisConnection _, string _, CancellationToken token, int? _, Func<int, Task> report) =>
			{
				await report(1);
				await Task.Delay(Timeout.Infinite, token);
				return Result.Success(1);
			});
		var user = new Mock<ICurrentUserService>();
		user.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
		var logs = new Mock<IAuditLogRepository>();
		var unit = new Mock<IUnitOfWork>();
		unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(1));
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddApplication();
		builder.Services.AddProblemDetails();
		builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuth>("Test", _ => { });
		builder.Services.AddAuthorization(options => options.AddPolicy(Policies.WriteAccess, policy => policy.RequireRole("User")));
		builder.Services.AddSingleton(repository.Object);
		builder.Services.AddSingleton(access.Object);
		builder.Services.AddSingleton(keys.Object);
		builder.Services.AddSingleton(user.Object);
		builder.Services.AddSingleton(logs.Object);
		builder.Services.AddSingleton(unit.Object);
		await using var app = builder.Build();
		app.UseRouting();
		app.UseAuthentication();
		app.UseAuthorization();
		new StreamDeleteKeysByPatternEndpoint().MapEndpoint(app);
		await app.StartAsync();
		var address = System.Linq.Enumerable.Single(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses);
		using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
		var operationId = Guid.NewGuid();
		using var request = new HttpRequestMessage(HttpMethod.Post, $"/storage/{connection.Id}/keys/pattern-delete/stream")
		{
			Content = JsonContent.Create(new { operationId, pattern = "test:*", database = 1 })
		};
		using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
		Assert.That(await reader.ReadLineAsync(), Is.EqualTo("event: progress"));
		Assert.That(await reader.ReadLineAsync(), Does.Contain("\"deletedCount\":1"));
		Assert.That(await reader.ReadLineAsync(), Is.Empty);
		using var wrongConnection = await client.PostAsJsonAsync($"/storage/{Guid.NewGuid()}/keys/pattern-delete/stop", new { operationId });
		Assert.That(wrongConnection.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		using var stop = await client.PostAsJsonAsync($"/storage/{connection.Id}/keys/pattern-delete/stop", new { operationId });
		Assert.That(stop.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(await reader.ReadLineAsync(), Is.EqualTo("event: complete"));
		var final = await reader.ReadLineAsync();
		Assert.That(final, Does.Contain("\"deletedCount\":1").And.Contain("\"cancelled\":true"));
		logs.Verify(x => x.Add(It.Is<AuditLog>(log => log.EntityId == connection.Id && log.Metadata.NewValue!.Contains("cancelled: True"))), Times.Once);
		await app.StopAsync();
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
