using Microsoft.AspNetCore.Authorization;

namespace RedisGUI.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement for connection ownership
/// </summary>
public sealed class ConnectionOwnershipRequirement : IAuthorizationRequirement
{
	/// <summary>
	/// Initializes a new instance of the ConnectionOwnershipRequirement
	/// </summary>
	public ConnectionOwnershipRequirement()
	{
	}
}
