using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using RedisGUI.Shared.Mediator;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.GetAllRedisValues;

/// <summary>
/// Handles the query to retrieve all Redis values with optional filtering and pagination.
/// </summary>
internal sealed class GetAllRedisValuesQueryHandler 
    : IQueryHandler<GetAllRedisKeysQuery, GetAllRedisValuesResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IRedisValueService valueService;

    /// <summary>
    /// Initializes a new instance of the GetAllRedisValuesQueryHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="valueService">Service for Redis value operations.</param>
    public GetAllRedisValuesQueryHandler(
        IRedisConnectionRepository connectionRepository,
        IRedisValueService valueService)
    {
        this.connectionRepository = connectionRepository;
        this.valueService = valueService;
    }

    /// <summary>
    /// Handles the query to retrieve all Redis values.
    /// </summary>
    /// <param name="request">The query request containing connection ID and filtering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the Redis values response or an error.</returns>
    public async Task<Result<GetAllRedisValuesResponse>> Handle(
        GetAllRedisKeysQuery request, 
        CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId, 
            cancellationToken);
        
        if (connectionResult.IsFailure)
        {
            return Result.Failure<GetAllRedisValuesResponse>(connectionResult.Error);
        }

        var result = await valueService.GetAllValuesAsync(
            connectionResult.Value, 
            request.Pattern,
            request.PageSize,
            request.PageNumber,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<GetAllRedisValuesResponse>(result.Error);
        }

        var pagedValues = result.Value;
        var response = new GetAllRedisValuesResponse(
            pagedValues.Values.Select(v => new RedisKeyValuePair(
                v.Key,
                v.Value,
                v.Type,
                v.TimeToLive)),
            pagedValues.TotalCount,
            pagedValues.PageNumber,
            pagedValues.PageSize,
            pagedValues.TotalPages,
            pagedValues.HasPreviousPage,
            pagedValues.HasNextPage);

        return Result.Success(response);
    }
} 
