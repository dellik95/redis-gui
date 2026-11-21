using FluentResults;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using DispatchWeave;
using DispatchWeave.Results;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

/// <summary>Deletes a pattern while reporting confirmed changes.</summary>
[Auditable(AuditAction.Delete, "Redis")]
public sealed record StreamDeleteKeysByPatternCommand(Guid ConnectionId, string Pattern, int Database,
	Func<int, Task> OnProgress) : ICommand<StreamDeleteKeysByPatternResult>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
	public bool PersistAuditOnCancellation => true;
}

public sealed record StreamDeleteKeysByPatternResult(int DeletedCount, bool Cancelled, string? Error) : IAuditOutcome
{
	public string FormatAuditOutcome() =>
		$"Deleted {DeletedCount} matching keys; cancelled: {Cancelled}; failed: {Error is not null}.";
}

public sealed class StreamDeleteKeysByPatternCommandValidator : AbstractValidator<StreamDeleteKeysByPatternCommand>
{
	public StreamDeleteKeysByPatternCommandValidator()
	{
		RuleFor(x => x.ConnectionId).NotEmpty();
		RuleFor(x => x.Pattern).NotEmpty().MaximumLength(256);
		RuleFor(x => x.Database).InclusiveBetween(0, 15);
		RuleFor(x => x.OnProgress).NotNull();
	}
}

internal sealed class StreamDeleteKeysByPatternCommandHandler(IRedisConnectionRepository repository,
	IConnectionAccessService access, IRedisKeyService keys) : ICommandHandler<StreamDeleteKeysByPatternCommand, StreamDeleteKeysByPatternResult>
{
	public async Task<Result<StreamDeleteKeysByPatternResult>> Handle(StreamDeleteKeysByPatternCommand request, CancellationToken cancellationToken)
	{
		var connection = await repository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);
		if (connection.IsFailed) return Result.Fail<StreamDeleteKeysByPatternResult>(connection.Errors);
		int deleted = 0;
		try
		{
			var result = await keys.DeleteKeysByPatternAsync(connection.Value, request.Pattern, cancellationToken, request.Database, async count =>
			{
				deleted = count;
				await request.OnProgress(count);
			});
			return Result.Ok(result.IsSuccess
				? new StreamDeleteKeysByPatternResult(result.Value, false, null)
				: new StreamDeleteKeysByPatternResult(deleted, false, result.GetDomainError().Message));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return Result.Ok(new StreamDeleteKeysByPatternResult(deleted, true, null));
		}
	}
}
