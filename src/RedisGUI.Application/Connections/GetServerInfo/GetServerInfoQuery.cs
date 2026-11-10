using System;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.GetServerInfo;

public sealed record GetServerInfoQuery(Guid ConnectionId) : IQuery<RedisServerInfoDto>;
