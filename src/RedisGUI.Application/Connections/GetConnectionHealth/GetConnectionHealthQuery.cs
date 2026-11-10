using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.GetConnectionHealth;

/// <summary>
/// Query to get health status of a connection
/// </summary>
/// <param name="ConnectionId">The connection identifier</param>
public sealed record GetConnectionHealthQuery(Guid ConnectionId) : IQuery<GetConnectionHealthResponse>;
