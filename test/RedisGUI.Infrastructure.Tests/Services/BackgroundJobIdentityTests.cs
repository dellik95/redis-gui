using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Infrastructure.Services;

namespace RedisGUI.Infrastructure.Tests.Services;

[TestFixture]
public class BackgroundJobIdentityTests
{
	[Test]
	public async Task BackgroundScope_UsesCapturedPrincipalWithoutHttpContext()
	{
		var userId = Guid.NewGuid();
		var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, "User")], "test"));
		var identity = new JobIdentity { Principal = principal };
		var http = new Mock<IHttpContextAccessor>();
		var current = new CurrentUserService(http.Object, identity);
		Assert.That(current.UserId, Is.EqualTo(userId));
		Assert.That(current.IsAuthenticated, Is.True);
		var authorization = new Mock<IAuthorizationService>();
		authorization.Setup(x => x.AuthorizeAsync(principal, It.IsAny<object>(), It.IsAny<System.Collections.Generic.IEnumerable<IAuthorizationRequirement>>()))
			.ReturnsAsync(AuthorizationResult.Success());
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		Assert.That(await new ConnectionAccessService(http.Object, authorization.Object, identity).CanAccessAsync(connection), Is.True);
	}
}
