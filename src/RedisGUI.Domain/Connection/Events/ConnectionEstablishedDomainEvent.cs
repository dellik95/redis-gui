using RedisGUI.Domain.Abstraction;
using System;

namespace RedisGUI.Domain.Connection.Events;

/// <summary>
/// Represents an event that is raised when a connection is successfully established
/// </summary>
public sealed class ConnectionEstablishedDomainEvent : IDomainEvent
{
    /// <summary>
    /// Creates a new instance of ConnectionEstablishedDomainEvent
    /// </summary>
    /// <param name="connectionId">The ID of the established connection</param>
    public ConnectionEstablishedDomainEvent(Guid connectionId)
    {
        ConnectionId = connectionId;
        OccurredOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the ID of the established connection
    /// </summary>
    public Guid ConnectionId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred
    /// </summary>
    public DateTime OccurredOn { get; }
} 