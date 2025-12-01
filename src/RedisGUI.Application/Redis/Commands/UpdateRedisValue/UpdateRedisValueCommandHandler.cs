using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Redis.Commands.UpdateRedisValue;

/// <summary>
/// Handles updating Redis values with type-specific logic.
/// </summary>
internal sealed class UpdateRedisValueCommandHandler : ICommandHandler<UpdateRedisValueCommand>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    public UpdateRedisValueCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    public async Task<Result> Handle(UpdateRedisValueCommand request, CancellationToken cancellationToken)
    {
        // Get connection
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            return connectionResult;
        }

        var connection = connectionResult.Value;

        // Convert type-specific value data to appropriate format
        object valueToSet = request.Value switch
        {
            StringValueData str => str.Value,
            ListValueData list => list.Items,
            SetValueData set => set.Members,
            HashValueData hash => hash.Fields,
            SortedSetValueData sorted => sorted.MembersWithScores,
            StreamValueData stream => stream.Entries.Select(e => e.Fields).ToList(),
            _ => null!
        };

        if (valueToSet == null)
        {
            return Result.Failure(new Error("RedisValue.InvalidData", "Invalid value data for the specified type"));
        }

        // Update the value using the connection service
        var result = await connectionService.UpdateValue(
            connection,
            request.Key,
            valueToSet,
            request.Type.ToString(),
            request.Ttl);

        return result;
    }
}
