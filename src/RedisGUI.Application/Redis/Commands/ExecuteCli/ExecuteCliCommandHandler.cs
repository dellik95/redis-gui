using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.ExecuteCli;

/// <summary>
/// Handler for executing a raw Redis CLI command
/// </summary>
internal sealed class ExecuteCliCommandHandler(
    IRedisConnectionRepository connectionRepository,
    IRedisCliService cliService) : ICommandHandler<ExecuteCliCommand, ExecuteCliResponse>
{
    public async Task<Result<ExecuteCliResponse>> Handle(ExecuteCliCommand request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
        if (connectionResult.IsFailure)
        {
            return Result.Failure<ExecuteCliResponse>(connectionResult.Error);
        }

        if (connectionResult.Value.IsReadOnly)
        {
            string cmd = request.Command.Trim().ToUpperInvariant();
            if (cmd.StartsWith("FLUSHALL") || cmd.StartsWith("FLUSHDB") || cmd.StartsWith("DEL") || cmd.StartsWith("UNLINK") || cmd.StartsWith("SET") || cmd.StartsWith("RENAME") || cmd.StartsWith("RESTORE"))
            {
                return Result.Failure<ExecuteCliResponse>(DomainErrors.Connection.ReadOnlyMode);
            }
        }

        var executionResult = await cliService.ExecuteCliCommandAsync(
            connectionResult.Value,
            request.Command,
            request.Database,
            cancellationToken);

        if (executionResult.IsFailure)
        {
            return Result.Failure<ExecuteCliResponse>(executionResult.Error);
        }

        return Result.Success(new ExecuteCliResponse(executionResult.Value, true));
    }
}
