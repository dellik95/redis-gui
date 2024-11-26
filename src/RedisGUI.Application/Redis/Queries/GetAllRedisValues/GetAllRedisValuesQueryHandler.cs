using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.GetAllRedisValues;

/// <summary>
/// Handles the query to retrieve all Redis values with optional filtering and pagination.
/// </summary>
internal sealed class GetAllRedisValuesQueryHandler 
    : IQueryHandler<GetAllRedisValuesQuery, GetAllRedisValuesResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the GetAllRedisValuesQueryHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public GetAllRedisValuesQueryHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the query to retrieve all Redis values.
    /// </summary>
    /// <param name="request">The query request containing connection ID and filtering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the Redis values response or an error.</returns>
    public async Task<Result<GetAllRedisValuesResponse>> Handle(
        GetAllRedisValuesQuery request, 
        CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId, 
            cancellationToken);
        
        if (connectionResult.IsFailure)
        {
            return Result.Failure<GetAllRedisValuesResponse>(connectionResult.Error);
        }

        var result = await connectionService.GetAllValues(
            connectionResult.Value, 
            request.Pattern,
            request.PageSize,
            request.PageNumber);

        if (result.IsFailure)
        {
            return Result.Failure<GetAllRedisValuesResponse>(result.Error);
        }

        var response = new GetAllRedisValuesResponse(
            result.Value.Values.Select(v => new RedisKeyValuePair(
                v.Key,
                v.Value,
                v.Type,
                v.TimeToLive)),
            result.Value.TotalCount,
            result.Value.PageNumber,
            result.Value.PageSize);

        return Result.Success(response);
    }
} 
