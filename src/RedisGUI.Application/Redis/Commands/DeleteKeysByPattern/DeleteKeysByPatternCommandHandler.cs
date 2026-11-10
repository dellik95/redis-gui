using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

/// <summary>
/// Handles the command to delete keys matching a pattern.
/// </summary>
internal sealed class DeleteKeysByPatternCommandHandler : ICommandHandler<DeleteKeysByPatternCommand, DeleteKeysByPatternResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IRedisKeyService keyService;

    public DeleteKeysByPatternCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IRedisKeyService keyService)
    {
        this.connectionRepository = connectionRepository;
        this.keyService = keyService;
    }

    public async Task<Result<DeleteKeysByPatternResponse>> Handle(
        DeleteKeysByPatternCommand request,
        CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            return Result.Failure<DeleteKeysByPatternResponse>(connectionResult.Error);
        }

        if (connectionResult.Value.IsReadOnly)
        {
            return Result.Failure<DeleteKeysByPatternResponse>(DomainErrors.Connection.ReadOnlyMode);
        }

        var deleteResult = await keyService.DeleteKeysByPatternAsync(
            connectionResult.Value,
            request.Pattern,
            cancellationToken,
            request.Database);

        if (deleteResult.IsFailure)
        {
            return Result.Failure<DeleteKeysByPatternResponse>(deleteResult.Error);
        }

        var deletedCount = deleteResult.Value;
        
        var response = new DeleteKeysByPatternResponse(
            deletedCount,
            request.Pattern);

        return Result.Success(response);
    }
}
