using System;
using RedisGUI.Domain.Users;

namespace RedisGUI.Application.Users.Commands.LoginUser;

/// <summary>
/// Response containing user authentication details
/// </summary>
/// <param name="UserId">The user's unique identifier</param>
/// <param name="Email">The user's email address</param>
/// <param name="FirstName">The user's first name</param>
/// <param name="LastName">The user's last name</param>
/// <param name="Role">The user's role</param>
/// <param name="Token">The JWT authentication token</param>
/// <param name="ExpiresAt">Token expiration timestamp</param>
public sealed record LoginUserResponse(
	Guid UserId,
	string Email,
	string FirstName,
	string LastName,
	UserRole Role,
	string Token,
	DateTime ExpiresAt);
