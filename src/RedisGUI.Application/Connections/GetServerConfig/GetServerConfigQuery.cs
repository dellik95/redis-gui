using System;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetServerConfig;

public sealed record GetServerConfigQuery(Guid ConnectionId, string? Pattern = null) : IQuery<RedisServerConfigDto>;

public sealed class GetServerConfigQueryHandler : IQueryHandler<GetServerConfigQuery, RedisServerConfigDto>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisDiagnosticsService diagnosticsService;

	public GetServerConfigQueryHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisDiagnosticsService diagnosticsService)
	{
		this.connectionRepository = connectionRepository;
		this.diagnosticsService = diagnosticsService;
	}

	public async Task<Result<RedisServerConfigDto>> Handle(GetServerConfigQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisServerConfigDto>(connectionResult.Error);
		}

		var connection = connectionResult.Value;
		if (connection == null)
		{
			return Result.Failure<RedisServerConfigDto>(DomainErrors.Connection.NotFound);
		}

		return await diagnosticsService.GetServerConfigAsync(connection, request.Pattern, cancellationToken);
	}
}
