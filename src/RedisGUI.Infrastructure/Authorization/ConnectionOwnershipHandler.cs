using Microsoft.AspNetCore.Authorization;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Users;
using System.Security.Claims;

namespace RedisGUI.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for connection ownership
/// </summary>
public sealed class ConnectionOwnershipHandler : AuthorizationHandler<ConnectionOwnershipRequirement, RedisConnection>
{
	private readonly ICurrentUserService _currentUserService;

	/// <summary>
	/// Initializes a new instance of the ConnectionOwnershipHandler
	/// </summary>
	public ConnectionOwnershipHandler(ICurrentUserService currentUserService)
	{
		_currentUserService = currentUserService;
	}

	/// <summary>
	/// Handles the authorization requirement
	/// </summary>
	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		ConnectionOwnershipRequirement requirement,
		RedisConnection resource)
	{
		// Get user ID from claims
		Guid? userId = _currentUserService.UserId;
		if (!userId.HasValue)
		{
			return Task.CompletedTask; // Not authenticated
		}

		// Get user role from claims
		string? roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
		if (!Enum.TryParse<UserRole>(roleClaim, out UserRole userRole))
		{
			userRole = UserRole.ReadOnly; // Default to most restrictive
		}

		// Check if user can access the connection using domain logic
		if (resource.CanBeAccessedBy(userId.Value, userRole))
		{
			context.Succeed(requirement);
		}

		return Task.CompletedTask;
	}
}
