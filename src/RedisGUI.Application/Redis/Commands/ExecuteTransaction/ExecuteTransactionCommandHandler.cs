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
	private readonly IRedisTransactionService transactionService;

	public ExecuteTransactionCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisTransactionService transactionService)
	{
		this.connectionRepository = connectionRepository;
		this.transactionService = transactionService;
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

		var result = await transactionService.ExecuteTransactionAsync(connectionResult.Value, request.Operations, cancellationToken);

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
