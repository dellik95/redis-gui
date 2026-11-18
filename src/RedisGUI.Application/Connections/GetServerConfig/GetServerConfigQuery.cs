using System;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetServerConfig;

public sealed record GetServerConfigQuery(Guid ConnectionId, string? Pattern = null) : IQuery<RedisServerConfigDto>;

public sealed class GetServerConfigQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisDiagnosticsService diagnosticsService,
	IConnectionAccessService? access = null) : IQueryHandler<GetServerConfigQuery, RedisServerConfigDto>
{
	public async Task<Result<RedisServerConfigDto>> Handle(GetServerConfigQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisServerConfigDto>(connectionResult.Error);
		}

		return await diagnosticsService.GetServerConfigAsync(connectionResult.Value, request.Pattern, cancellationToken);
	}
}
