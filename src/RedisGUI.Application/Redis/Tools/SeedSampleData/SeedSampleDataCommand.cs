using FluentResults;
using RedisGUI.Domain.Redis;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
using System;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.SeedSampleData;

/// <summary>
/// Command to populate Redis with rich configurable demo/sample data across multiple databases
/// </summary>
[Auditable(AuditAction.Create, "Redis")]
public sealed record SeedSampleDataCommand(
	Guid ConnectionId,
	SeedSampleDataOptions? Options = null,
	Func<int, int, Task>? OnProgress = null,
	CancellationToken? OperationCancellation = null) : ICommand<SeedDataResult>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
	public bool PersistAuditOnCancellation => true;
}

internal sealed class SeedSampleDataCommandHandler : ICommandHandler<SeedSampleDataCommand, SeedDataResult>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisSeedDataService seedDataService;
	private readonly IConnectionAccessService access;

	public SeedSampleDataCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisSeedDataService seedDataService, IConnectionAccessService access)
	{
		this.connectionRepository = connectionRepository;
		this.seedDataService = seedDataService;
		this.access = access;
	}

	public async Task<Result<SeedDataResult>> Handle(SeedSampleDataCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailed)
		{
			return Result.Fail<SeedDataResult>(connectionResult.Errors);
		}

		if (!await access.CanAccessAsync(connectionResult.Value))
			return Result.Fail<SeedDataResult>(new DomainError("Connection.Forbidden", "Access to the connection is denied."));

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Fail<SeedDataResult>(DomainErrors.Connection.ReadOnlyMode);
		}

		return await seedDataService.SeedSampleDataAsync(connectionResult.Value, request.Options,
			request.OperationCancellation ?? cancellationToken, request.OnProgress);
	}
}
