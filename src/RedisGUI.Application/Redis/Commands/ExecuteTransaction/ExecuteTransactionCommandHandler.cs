using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.ExecuteTransaction;

/// <summary>
/// Handler for executing Redis transactions.
/// </summary>
public sealed class ExecuteTransactionCommandHandler : ICommandHandler<ExecuteTransactionCommand, ExecuteTransactionResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IConnectionService connectionService;

	public ExecuteTransactionCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IConnectionService connectionService)
	{
		this.connectionRepository = connectionRepository;
		this.connectionService = connectionService;
	}

	public async Task<Result<ExecuteTransactionResponse>> Handle(
		ExecuteTransactionCommand request,
		CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		
		if (connectionResult.IsFailure)
		{
			return Result.Failure<ExecuteTransactionResponse>(connectionResult.Error);
		}

		var result = await connectionService.ExecuteTransaction(connectionResult.Value, request.Operations);

		if (result.IsFailure)
		{
			return Result.Failure<ExecuteTransactionResponse>(result.Error);
		}

		var results = result.Value;
		var response = new ExecuteTransactionResponse(
			Success: true,
			Results: results,
			OperationsExecuted: results.Count);

		return Result.Success(response);
	}
}
