using System;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetServerInfo;

public sealed record GetServerInfoQuery(Guid ConnectionId) : IQuery<RedisServerInfoDto>;

public sealed class GetServerInfoQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisDiagnosticsService diagnosticsService,
	IConnectionAccessService? access = null) : IQueryHandler<GetServerInfoQuery, RedisServerInfoDto>
{
	public async Task<Result<RedisServerInfoDto>> Handle(GetServerInfoQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisServerInfoDto>(connectionResult.Error);
		}

		return await diagnosticsService.GetServerInfoAsync(connectionResult.Value, cancellationToken);
	}
}
