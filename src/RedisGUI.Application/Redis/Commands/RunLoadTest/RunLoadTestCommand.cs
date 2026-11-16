using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.RunLoadTest;

/// <summary>
/// Command to run a synthetic load test against Redis with guaranteed test key cleanup
/// </summary>
public sealed record RunLoadTestCommand(
	Guid ConnectionId,
	int OperationsCount = 1000,
	int Concurrency = 5,
	string OperationType = "mixed",
	int PayloadSizeBytes = 128,
	int? Database = null,
	string? KeyPrefix = null,
	int DurationSeconds = 0) : ICommand<RedisLoadTestResult>;

internal sealed class RunLoadTestCommandHandler : ICommandHandler<RunLoadTestCommand, RedisLoadTestResult>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisLoadTestService loadTestService;

	public RunLoadTestCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisLoadTestService loadTestService)
	{
		this.connectionRepository = connectionRepository;
		this.loadTestService = loadTestService;
	}

	public async Task<Result<RedisLoadTestResult>> Handle(RunLoadTestCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisLoadTestResult>(connectionResult.Error);
		}

		var connection = connectionResult.Value;

		if (connection.IsReadOnly && !string.Equals(request.OperationType, "read", System.StringComparison.OrdinalIgnoreCase))
		{
			return Result.Failure<RedisLoadTestResult>(DomainErrors.Connection.ReadOnlyMode);
		}

		var testRequest = new RedisLoadTestRequest(
			request.OperationsCount,
			request.Concurrency,
			request.OperationType,
			request.PayloadSizeBytes,
			request.Database,
			request.KeyPrefix,
			request.DurationSeconds);

		return await loadTestService.RunLoadTestAsync(connection, testRequest, cancellationToken);
	}
}
