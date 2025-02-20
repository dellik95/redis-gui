using RedisGUI.Domain.Abstraction;
using System;

namespace RedisGUI.Domain.Connection.Events
{
	/// <summary>
	/// Represents an event that is raised when a new connection availability is changed.
	/// </summary>
	public class ConnectionAvailabilityChanged : IDomainEvent
	{
		/// <summary>
		/// Creates a new instance of ConnectionAvailabilityChanged.
		/// </summary>
		/// <param name="connectionId">The ID of the created connection.</param>
		/// <param name="isAvailable">The state of connection.</param>
		public ConnectionAvailabilityChanged(Guid connectionId, bool isAvailable)
		{
			this.ConnectionId = connectionId;
			IsAvailable = isAvailable;
			this.OccurredOn = DateTime.UtcNow;
		}


		/// <summary>
		/// Gets the ID of the established connection
		/// </summary>
		public Guid ConnectionId { get; }

		/// <summary>
		/// Gets the current availability state.
		/// </summary>
		public bool IsAvailable { get; }

		/// <summary>
		/// Gets the UTC timestamp when the event occurred
		/// </summary>
		public DateTime OccurredOn { get; }
	}
}
