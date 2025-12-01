using System;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

/// <summary>
/// Handles the command to delete Redis keys by pattern.
/// </summary>
internal sealed class DeleteKeysByPatternCommandHandler
    : ICommandHandler<DeleteKeysByPatternCommand, DeleteKeysByPatternResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the DeleteKeysByPatternCommandHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public DeleteKeysByPatternCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the command to delete keys by pattern.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the response or an error.</returns>
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

        var deleteResult = await connectionService.DeleteKeysByPattern(
            connectionResult.Value,
            request.Pattern);

        if (deleteResult.IsFailure)
        {
            return Result.Failure<DeleteKeysByPatternResponse>(deleteResult.Error);
        }

        var response = new DeleteKeysByPatternResponse(
            deleteResult.Value,
            request.Pattern);

        return Result.Success(response);
    }
}
