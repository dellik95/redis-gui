using RedisGUI.Application.Abstraction.Messaging;
using System;
using RedisGUI.Application.Connections.Shared;

namespace RedisGUI.Application.Connections.GetConnectionById;

/// <summary>
/// Query to retrieve a Redis connection by its unique identifier.
/// </summary>
/// <param name="id">The unique identifier of the connection to retrieve</param>
public sealed record GetConnectionByIdQuery(Guid id) : IQuery<GetConnectionResponse>;
