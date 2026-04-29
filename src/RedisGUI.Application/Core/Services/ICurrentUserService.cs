using System;

namespace RedisGUI.Application.Core.Services;

/// <summary>
/// Service for accessing current user information
/// </summary>
public interface ICurrentUserService
{
	/// <summary>
	/// Gets the current user's ID (null if anonymous)
	/// </summary>
	Guid? UserId { get; }

	/// <summary>
	/// Gets the current user's IP address
	/// </summary>
	string? IpAddress { get; }

	/// <summary>
	/// Gets the current user's user agent
	/// </summary>
	string? UserAgent { get; }

	/// <summary>
	/// Gets whether the user is authenticated
	/// </summary>
	bool IsAuthenticated { get; }
}
