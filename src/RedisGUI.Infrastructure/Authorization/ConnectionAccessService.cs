using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RedisGUI.Application.Core.Services;

namespace RedisGUI.Infrastructure.Authorization;

internal sealed class ConnectionAccessService(IHttpContextAccessor contextAccessor, IAuthorizationService authorization) : IConnectionAccessService
{
	public async Task<bool> CanAccessAsync(RedisConnection connection)
	{
		var user = contextAccessor.HttpContext?.User;
		return user is not null && (await authorization.AuthorizeAsync(user, connection, new ConnectionOwnershipRequirement())).Succeeded;
	}
}
