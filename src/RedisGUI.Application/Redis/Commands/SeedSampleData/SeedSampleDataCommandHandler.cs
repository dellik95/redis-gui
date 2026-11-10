using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.SeedSampleData;

internal sealed class SeedSampleDataCommandHandler : ICommandHandler<SeedSampleDataCommand, SeedDataResult>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisSeedDataService seedDataService;

	public SeedSampleDataCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisSeedDataService seedDataService)
	{
		this.connectionRepository = connectionRepository;
		this.seedDataService = seedDataService;
	}

	public async Task<Result<SeedDataResult>> Handle(SeedSampleDataCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<SeedDataResult>(connectionResult.Error);
		}

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Failure<SeedDataResult>(DomainErrors.Connection.ReadOnlyMode);
		}

		return await seedDataService.SeedSampleDataAsync(connectionResult.Value, request.Options, cancellationToken);
	}
}
