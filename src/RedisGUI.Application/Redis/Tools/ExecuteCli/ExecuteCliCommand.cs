using FluentResults;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using FluentValidation;
using System;
using DispatchWeave;
using DispatchWeave.Results;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.ExecuteCli;

/// <summary>
/// Command to execute a raw Redis CLI command
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier</param>
/// <param name="Command">The raw command string</param>
/// <param name="Database">Optional target database</param>
[Auditable(AuditAction.RedisCommandExecuted, "Redis")]
public sealed record ExecuteCliCommand(
	Guid ConnectionId,
	string Command,
	int? Database = null) : ICommand<ExecuteCliResponse>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

internal sealed class ExecuteCliCommandValidator : AbstractValidator<ExecuteCliCommand>
{
	public ExecuteCliCommandValidator() => RuleFor(request => request.Command).NotEmpty().WithMessage("Command cannot be empty.");
}

/// <summary>
/// Handler for executing a raw Redis CLI command
/// </summary>
internal sealed class ExecuteCliCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisCliService cliService,
	IConnectionAccessService? access = null) : ICommandHandler<ExecuteCliCommand, ExecuteCliResponse>
{
	public async Task<Result<ExecuteCliResponse>> Handle(ExecuteCliCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailed)
		{
			return Result.Fail<ExecuteCliResponse>(connectionResult.Errors);
		}

		if (connectionResult.Value.IsReadOnly)
		{
			string cmd = request.Command.Trim().ToUpperInvariant();
			if (cmd.StartsWith("FLUSHALL") || cmd.StartsWith("FLUSHDB") || cmd.StartsWith("DEL") || cmd.StartsWith("UNLINK") || cmd.StartsWith("SET") || cmd.StartsWith("RENAME") || cmd.StartsWith("RESTORE"))
			{
				return Result.Fail<ExecuteCliResponse>(DomainErrors.Connection.ReadOnlyMode);
			}
		}

		var executionResult = await cliService.ExecuteCliCommandAsync(
			connectionResult.Value,
			request.Command,
			request.Database,
			cancellationToken);

		if (executionResult.IsFailed)
		{
			return Result.Fail<ExecuteCliResponse>(executionResult.Errors);
		}

		return Result.Ok(new ExecuteCliResponse(executionResult.Value, true));
	}
}
