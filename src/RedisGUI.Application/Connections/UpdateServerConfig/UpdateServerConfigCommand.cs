using RedisGUI.Domain.Audit;
using System;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.UpdateServerConfig;

[Auditable(AuditAction.Update, "Redis")]
public sealed record UpdateServerConfigCommand(
	Guid ConnectionId,
	string ParameterName,
	string ParameterValue) : ICommand<RedisSetConfigResponseDto>;
