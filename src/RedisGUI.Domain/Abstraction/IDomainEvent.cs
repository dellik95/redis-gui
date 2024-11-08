using MediatR;

namespace RedisGUI.Domain.Abstraction;

/// <summary>
/// Represents a domain event that can be published and handled within the application
/// </summary>
public interface IDomainEvent : INotification
{
}
