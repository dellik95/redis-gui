using System;
using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Connection.Events
{
    /// <summary>
    /// Represents an event that is raised when a new connection is created
    /// </summary>
    public sealed class ConnectionCreatedDomainEvent : IDomainEvent
    {
        /// <summary>
        /// Creates a new instance of ConnectionCreatedDomainEvent
        /// </summary>
        /// <param name="connectionId">The ID of the created connection</param>
        /// <param name="connectionName">The name of the created connection</param>
        public ConnectionCreatedDomainEvent(Guid connectionId, string connectionName)
        {
            ConnectionId = connectionId;
            ConnectionName = connectionName;
            OccurredOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the created connection
        /// </summary>
        public Guid ConnectionId { get; }

        /// <summary>
        /// Gets the name of the created connection
        /// </summary>
        public string ConnectionName { get; }

        /// <summary>
        /// Gets the UTC timestamp when the event occurred
        /// </summary>
        public DateTime OccurredOn { get; }
    }
} 
