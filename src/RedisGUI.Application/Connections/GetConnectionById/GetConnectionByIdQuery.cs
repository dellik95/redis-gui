using RedisGUI.Application.Abstraction.Messaging;

namespace RedisGUI.Application.Connections.GetConnectionById;

public sealed record GetConnectionByIdQuery(Guid id) : IQuery<GetConnectionByIdResponse>;