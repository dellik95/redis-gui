using System;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Domain.Audit;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Users.Commands.RegisterUser;

/// <summary>
/// Command to register a new user in the system
/// </summary>
/// <param name="Email">The user's email address</param>
/// <param name="Password">The user's password</param>
/// <param name="FirstName">The user's first name</param>
/// <param name="LastName">The user's last name</param>
[Auditable(AuditAction.Create, "User")]
public sealed record RegisterUserCommand(
	string Email,
	string Password,
	string FirstName,
	string LastName) : ICommand<Guid>;
