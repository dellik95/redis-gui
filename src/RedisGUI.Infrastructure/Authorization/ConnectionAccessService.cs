using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RedisGUI.Application.Core.Services;
using RedisGUI.Infrastructure.BackgroundJobs;

namespace RedisGUI.Infrastructure.Authorization;

internal sealed class ConnectionAccessService(IHttpContextAccessor contextAccessor, IAuthorizationService authorization, JobIdentity identity) : IConnectionAccessService
{
	public async Task<bool> CanAccessAsync(RedisConnection connection)
	{
		var user = contextAccessor.HttpContext?.User ?? identity.Principal;
		return user is not null && (await authorization.AuthorizeAsync(user, connection, new ConnectionOwnershipRequirement())).Succeeded;
	}
}
