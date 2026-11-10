using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
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

    public DeleteRedisKeyCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IRedisKeyService keyService)
    {
        this.connectionRepository = connectionRepository;
        this.keyService = keyService;
    }

    public async Task<Result<bool>> Handle(DeleteRedisKeyCommand request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId, 
            cancellationToken);
        
        if (connectionResult.IsFailure)
        {
            return Result.Failure<bool>(connectionResult.Error);
        }

        if (connectionResult.Value.IsReadOnly)
        {
            return Result.Failure<bool>(DomainErrors.Connection.ReadOnlyMode);
        }

        return await keyService.DeleteKeyAsync(connectionResult.Value, request.Key, cancellationToken, request.Database);
    }
}
