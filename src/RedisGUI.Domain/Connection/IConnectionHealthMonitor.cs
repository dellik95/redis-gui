using System;
using System.Collections.Generic;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for monitoring Redis connection health
/// </summary>
public interface IConnectionHealthMonitor
{
	/// <summary>
	/// Get the current health status for a connection
	/// </summary>
	/// <param name="connectionId">Connection ID</param>
	/// <returns>Health information or null if not available</returns>
	ConnectionHealth? GetHealth(Guid connectionId);

	/// <summary>
	/// Get all connection health statuses
	/// </summary>
	/// <returns>Collection of health information</returns>
	IEnumerable<ConnectionHealth> GetAllHealth();
}
