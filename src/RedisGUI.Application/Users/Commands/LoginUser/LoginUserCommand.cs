using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Domain.Audit;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Users.Commands.LoginUser;

/// <summary>
/// Command to authenticate a user
/// </summary>
/// <param name="Email">The user's email address</param>
/// <param name="Password">The user's password</param>
[Auditable(AuditAction.Login, "User")]
public sealed record LoginUserCommand(
	string Email,
	string Password) : ICommand<LoginUserResponse>;
