using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.GetRedisValue;

/// <summary>
/// Handles the query to retrieve a single Redis value.
/// </summary>
internal sealed class GetRedisValueQueryHandler : IQueryHandler<GetRedisValueQuery, GetRedisValueResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the GetRedisValueQueryHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public GetRedisValueQueryHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the query to retrieve a Redis value by key.
    /// </summary>
    /// <param name="request">The query request containing the key to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the Redis value response or an error.</returns>
    public async Task<Result<GetRedisValueResponse>> Handle(GetRedisValueQuery request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
        
        if (connectionResult.IsFailure)
        {
            return Result.Failure<GetRedisValueResponse>(connectionResult.Error);
        }

        var result = await connectionService.GetValue(connectionResult.Value, request.Key);
        
        if (result.IsFailure)
        {
            return Result.Failure<GetRedisValueResponse>(result.Error);
        }

        return Result.Success(new GetRedisValueResponse(
            request.Key,
            result.Value.Value,
            result.Value.TimeToLive));
    }
} 
