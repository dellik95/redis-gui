using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.SetRedisValue;

/// <summary>
/// Handles the command to set a Redis key-value pair.
/// </summary>
internal sealed class SetRedisValueCommandHandler : ICommandHandler<SetRedisValueCommand>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the SetRedisValueCommandHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public SetRedisValueCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the command to set a Redis value.
    /// </summary>
    /// <param name="request">The command request containing the key, value, and optional TTL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public async Task<Result> Handle(SetRedisValueCommand request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
        
        if (connectionResult.IsFailure)
        {
            return Result.Failure(connectionResult.Error);
        }

        return await connectionService.SetValue(connectionResult.Value, request.Key, request.Value, request.Ttl);
    }
} 
