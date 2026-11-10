using System;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Application.Connections.GetConnectionHealth;

/// <summary>
/// Response containing connection health information
/// </summary>
/// <param name="ConnectionId">The connection identifier</param>
/// <param name="Status">Current health status</param>
/// <param name="LastChecked">Last check timestamp</param>
/// <param name="ResponseTimeMs">Response time in milliseconds</param>
/// <param name="ErrorMessage">Error message if unhealthy</param>
/// <param name="ConsecutiveFailures">Number of consecutive failures</param>
public sealed record GetConnectionHealthResponse(
	Guid ConnectionId,
	string Status,
	DateTime LastChecked,
	double ResponseTimeMs,
	string? ErrorMessage,
	int ConsecutiveFailures);
