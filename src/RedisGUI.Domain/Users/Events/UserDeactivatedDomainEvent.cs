using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Users.Events;

/// <summary>
/// Domain event raised when a user is deactivated
/// </summary>
/// <param name="UserId">The unique identifier of the deactivated user</param>
public sealed record UserDeactivatedDomainEvent(System.Guid UserId) : IDomainEvent;
