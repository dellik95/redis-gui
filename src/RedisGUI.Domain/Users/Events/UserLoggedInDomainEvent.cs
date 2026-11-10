using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Users.Events;

/// <summary>
/// Domain event raised when a user logs in
/// </summary>
/// <param name="UserId">The unique identifier of the user</param>
/// <param name="LoginAt">The timestamp of the login</param>
public sealed record UserLoggedInDomainEvent(System.Guid UserId, System.DateTime LoginAt) : IDomainEvent;
