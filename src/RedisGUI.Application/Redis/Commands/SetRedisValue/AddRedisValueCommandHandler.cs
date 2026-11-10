using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.SetRedisValue;

/// <summary>
/// Handles the command to set a Redis key-value pair.
/// </summary>
internal sealed class AddRedisValueCommandHandler : ICommandHandler<AddRedisValueCommand>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IRedisValueService valueService;

    /// <summary>
    /// Initializes a new instance of the AddRedisValueCommandHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="valueService">Service for Redis value operations.</param>
    public AddRedisValueCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IRedisValueService valueService)
    {
        this.connectionRepository = connectionRepository;
        this.valueService = valueService;
    }

    /// <summary>
    /// Handles the command to set a Redis value.
    /// </summary>
    /// <param name="request">The command request containing the key, value, and optional TTL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public async Task<Result> Handle(AddRedisValueCommand request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
        
        if (connectionResult.IsFailure)
        {
            return Result.Failure(connectionResult.Error);
        }

        return await valueService.SetValueAsync(connectionResult.Value, request.Key, request.Value, request.Ttl, cancellationToken);
    }
} 
