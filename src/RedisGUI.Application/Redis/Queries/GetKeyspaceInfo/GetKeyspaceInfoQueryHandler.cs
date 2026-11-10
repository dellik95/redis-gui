using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.GetKeyspaceInfo;

/// <summary>
/// Query handler for retrieving keyspace statistics across databases
/// </summary>
internal sealed class GetKeyspaceInfoQueryHandler(
    IRedisConnectionRepository connectionRepository,
    IRedisDiagnosticsService diagnosticsService) : IQueryHandler<GetKeyspaceInfoQuery, GetKeyspaceInfoResponse>
{
    public async Task<Result<GetKeyspaceInfoResponse>> Handle(GetKeyspaceInfoQuery request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
        if (connectionResult.IsFailure)
        {
            return Result.Failure<GetKeyspaceInfoResponse>(connectionResult.Error);
        }

        var keyspaceResult = await diagnosticsService.GetKeyspaceInfoAsync(connectionResult.Value, cancellationToken);
        if (keyspaceResult.IsFailure)
        {
            return Result.Failure<GetKeyspaceInfoResponse>(keyspaceResult.Error);
        }

        return Result.Success(new GetKeyspaceInfoResponse(keyspaceResult.Value));
    }
}
