using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using System;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.UpdateServerConfig;

[Auditable(AuditAction.Update, "Redis")]
public sealed record UpdateServerConfigCommand(
	Guid ConnectionId,
	string ParameterName,
	string ParameterValue) : ICommand<RedisSetConfigResponseDto>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

public sealed class UpdateServerConfigCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisDiagnosticsService diagnosticsService,
	IConnectionAccessService? access = null) : ICommandHandler<UpdateServerConfigCommand, RedisSetConfigResponseDto>
{
	private static readonly Error ReadOnlyError = new(
		"Connection.ReadOnly",
		"This connection is in read-only safe mode. Configuration changes are disabled.");

	public async Task<Result<RedisSetConfigResponseDto>> Handle(UpdateServerConfigCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true,
			readOnlyError: ReadOnlyError);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisSetConfigResponseDto>(connectionResult.Error);
		}

		return await diagnosticsService.SetServerConfigAsync(
			connectionResult.Value,
			request.ParameterName,
			request.ParameterValue,
			cancellationToken);
	}
}
