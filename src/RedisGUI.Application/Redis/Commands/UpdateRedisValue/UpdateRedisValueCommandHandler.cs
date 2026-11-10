using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.UpdateRedisValue;

/// <summary>
/// Handles the command to update a Redis value with type-specific handling
/// </summary>
internal sealed class UpdateRedisValueCommandHandler : ICommandHandler<UpdateRedisValueCommand>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IRedisValueService valueService;

    public UpdateRedisValueCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IRedisValueService valueService)
    {
        this.connectionRepository = connectionRepository;
        this.valueService = valueService;
    }

    public async Task<Result> Handle(UpdateRedisValueCommand request, CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            return connectionResult;
        }

        var connection = connectionResult.Value;

        if (connection.IsReadOnly)
        {
            return Result.Failure(DomainErrors.Connection.ReadOnlyMode);
        }

        object? valueToSet = request.Type switch
        {
            RedisDataType.String => request.Value.Value ?? string.Empty,
            RedisDataType.List => (object?)request.Value.Items ?? new List<string>(),
            RedisDataType.Set => (object?)request.Value.Members ?? new HashSet<string>(),
            RedisDataType.Hash => (object?)request.Value.Fields ?? new Dictionary<string, string>(),
            RedisDataType.SortedSet => (object?)request.Value.MembersWithScores ?? new Dictionary<string, double>(),
            RedisDataType.Stream => (object?)request.Value.Entries?.Select(e => e.Fields).ToList() ?? new List<Dictionary<string, string>>(),
            _ => null
        };

        if (valueToSet == null)
        {
            return Result.Failure(new Error("RedisValue.InvalidData", "Invalid value data for the specified type"));
        }

        return await valueService.UpdateValueAsync(
            connection,
            request.Key,
            valueToSet,
            request.Type.ToString(),
            request.Ttl,
            cancellationToken,
            request.Database);
    }
}
