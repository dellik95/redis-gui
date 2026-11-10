using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.GetSlowlog;

internal sealed class GetSlowlogQueryHandler : IQueryHandler<GetSlowlogQuery, List<SlowlogEntryInfo>>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisDiagnosticsService diagnosticsService;

	public GetSlowlogQueryHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisDiagnosticsService diagnosticsService)
	{
		this.connectionRepository = connectionRepository;
		this.diagnosticsService = diagnosticsService;
	}

	public async Task<Result<List<SlowlogEntryInfo>>> Handle(GetSlowlogQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<List<SlowlogEntryInfo>>(connectionResult.Error);
		}

		return await diagnosticsService.GetSlowlogAsync(connectionResult.Value, request.Count, cancellationToken);
	}
}
