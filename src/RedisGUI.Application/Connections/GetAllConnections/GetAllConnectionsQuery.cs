using System.Collections.Generic;
using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Application.Connections.Shared;

namespace RedisGUI.Application.Connections.GetAllConnections;

/// <summary>
///    Query to retrieve a Redis connections.
/// </summary>
/// <param name="PageNumber">Page number</param>
/// <param name="PageSize">Items count per page</param>
public record GetAllConnectionsQuery(int PageNumber, int PageSize) : IQuery<IEnumerable<GetConnectionResponse>>;
