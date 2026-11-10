using System;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.GetServerConfig;

public sealed record GetServerConfigQuery(Guid ConnectionId, string? Pattern = null) : IQuery<RedisServerConfigDto>;
