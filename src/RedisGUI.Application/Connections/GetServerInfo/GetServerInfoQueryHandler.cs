using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetServerInfo;

public sealed class GetServerInfoQueryHandler : IQueryHandler<GetServerInfoQuery, RedisServerInfoDto>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisDiagnosticsService diagnosticsService;

	public GetServerInfoQueryHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisDiagnosticsService diagnosticsService)
	{
		this.connectionRepository = connectionRepository;
		this.diagnosticsService = diagnosticsService;
	}

	public async Task<Result<RedisServerInfoDto>> Handle(GetServerInfoQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisServerInfoDto>(connectionResult.Error);
		}

		var connection = connectionResult.Value;
		if (connection == null)
		{
			return Result.Failure<RedisServerInfoDto>(DomainErrors.Connection.NotFound);
		}

		return await diagnosticsService.GetServerInfoAsync(connection, cancellationToken);
	}
}
