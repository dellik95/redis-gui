using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Users.Events;

/// <summary>
/// Domain event raised when a user changes their password
/// </summary>
/// <param name="UserId">The unique identifier of the user</param>
public sealed record UserPasswordChangedDomainEvent(System.Guid UserId) : IDomainEvent;
