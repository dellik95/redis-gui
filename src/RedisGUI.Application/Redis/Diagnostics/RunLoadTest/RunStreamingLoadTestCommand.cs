using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.RunLoadTest;

public sealed record RunStreamingLoadTestCommand(Guid ConnectionId, RedisLoadTestRequest Request,
	Func<RedisLoadTestProgress, Task>? OnProgress) : ICommand<RedisLoadTestResult>;

internal sealed class RunStreamingLoadTestCommandHandler(IRedisConnectionRepository repository, IConnectionAccessService access,
	IRedisLoadTestService loadTests) : ICommandHandler<RunStreamingLoadTestCommand, RedisLoadTestResult>
{
	public async Task<Result<RedisLoadTestResult>> Handle(RunStreamingLoadTestCommand request, CancellationToken cancellationToken)
	{
		return await (await repository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken)).BindAsync(async connection =>
		{
			if (!await access.CanAccessAsync(connection))
				return Result.Failure<RedisLoadTestResult>(new Error("Connection.Forbidden", "Access to the connection is denied."));
			if (connection.IsReadOnly && !string.Equals(request.Request.OperationType, "read", StringComparison.OrdinalIgnoreCase))
				return Result.Failure<RedisLoadTestResult>(DomainErrors.Connection.ReadOnlyMode);
			return await loadTests.RunStreamingLoadTestAsync(connection, request.Request, request.OnProgress, cancellationToken);
		});
	}
}
