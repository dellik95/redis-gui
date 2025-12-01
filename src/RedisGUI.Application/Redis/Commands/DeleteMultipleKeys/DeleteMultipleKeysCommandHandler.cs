using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;

/// <summary>
/// Handles the command to delete multiple Redis keys.
/// </summary>
internal sealed class DeleteMultipleKeysCommandHandler
    : ICommandHandler<DeleteMultipleKeysCommand, DeleteMultipleKeysResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the DeleteMultipleKeysCommandHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public DeleteMultipleKeysCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the command to delete multiple keys.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the response or an error.</returns>
    public async Task<Result<DeleteMultipleKeysResponse>> Handle(
        DeleteMultipleKeysCommand request,
        CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            return Result.Failure<DeleteMultipleKeysResponse>(connectionResult.Error);
        }

        var deleteResult = await connectionService.DeleteMultipleKeys(
            connectionResult.Value,
            request.Keys);

        if (deleteResult.IsFailure)
        {
            return Result.Failure<DeleteMultipleKeysResponse>(deleteResult.Error);
        }

        var (deletedCount, failedKeys) = deleteResult.Value;
        
        var response = new DeleteMultipleKeysResponse(
            deletedCount,
            request.Keys.Count,
            failedKeys);

        return Result.Success(response);
    }
}
