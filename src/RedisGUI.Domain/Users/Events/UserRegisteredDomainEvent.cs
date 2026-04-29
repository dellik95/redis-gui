using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Users.Events;

/// <summary>
/// Domain event raised when a new user registers
/// </summary>
/// <param name="UserId">The unique identifier of the registered user</param>
/// <param name="Email">The email address of the user</param>
public sealed record UserRegisteredDomainEvent(System.Guid UserId, string Email) : IDomainEvent;
