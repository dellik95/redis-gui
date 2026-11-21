using FluentResults;
using System;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Redis;
using DispatchWeave;
using DispatchWeave.Results;
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
		if (connectionResult.IsFailed)
		{
			return Result.Fail<RedisServerConfigDto>(connectionResult.Errors);
		}

		return await diagnosticsService.GetServerConfigAsync(connectionResult.Value, request.Pattern, cancellationToken);
	}
}
