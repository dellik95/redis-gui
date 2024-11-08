using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DeleteRedisKey;

/// <summary>
/// Handles the command to delete a Redis key.
/// </summary>
internal sealed class DeleteRedisKeyCommandHandler : ICommandHandler<DeleteRedisKeyCommand, bool>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the DeleteRedisKeyCommandHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public DeleteRedisKeyCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the command to delete a Redis key.
    /// </summary>
    /// <param name="request">The command request containing the key to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing a boolean indicating whether the key was deleted.</returns>
    public async Task<Result<bool>> Handle(DeleteRedisKeyCommand request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId, 
            cancellationToken);
        
        if (connectionResult.IsFailure)
        {
            return Result.Failure<bool>(connectionResult.Error);
        }

        return await connectionService.DeleteKey(connectionResult.Value, request.Key);
    }
} 
