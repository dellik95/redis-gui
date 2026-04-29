using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DeleteRedisKey;

/// <summary>
/// Handles the command to delete a Redis key.
/// </summary>
internal sealed class DeleteRedisKeyCommandHandler : ICommandHandler<DeleteRedisKeyCommand, bool>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IRedisKeyService keyService;

    /// <summary>
    /// Initializes a new instance of the DeleteRedisKeyCommandHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="keyService">Service for Redis key operations.</param>
    public DeleteRedisKeyCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IRedisKeyService keyService)
    {
        this.connectionRepository = connectionRepository;
        this.keyService = keyService;
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

        return await keyService.DeleteKeyAsync(connectionResult.Value, request.Key, cancellationToken);
    }
} 
