using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.BulkUpdateTtl;

internal sealed class BulkUpdateTtlCommandHandler : ICommandHandler<BulkUpdateTtlCommand, int>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisValueService valueService;

	public BulkUpdateTtlCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisValueService valueService)
	{
		this.connectionRepository = connectionRepository;
		this.valueService = valueService;
	}

	public async Task<Result<int>> Handle(BulkUpdateTtlCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<int>(connectionResult.Error);
		}

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Failure<int>(DomainErrors.Connection.ReadOnlyMode);
		}

		TimeSpan? ttl = request.TtlSeconds.HasValue && request.TtlSeconds.Value > 0
			? TimeSpan.FromSeconds(request.TtlSeconds.Value)
			: null;

		return await valueService.BulkUpdateTtlAsync(
			connectionResult.Value,
			request.Keys,
			ttl,
			request.Database,
			cancellationToken);
	}
}
