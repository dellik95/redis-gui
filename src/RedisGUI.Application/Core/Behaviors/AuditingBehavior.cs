using RedisGUI.Application.Core.Attributes;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Users.Commands.LoginUser;
using RedisGUI.Domain.Redis;
using RedisGUI.Application.Redis.Commands.ImportRedisData;
using RedisGUI.Application.Redis.Commands.BulkUpdateTtl;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically creates audit logs for commands
/// </summary>
public sealed class AuditingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	private readonly IAuditLogRepository auditLogRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly ICurrentUserService currentUserService;

	/// <summary>
	/// Initializes a new instance of the AuditingBehavior
	/// </summary>
	public AuditingBehavior(
		IAuditLogRepository auditLogRepository,
		IUnitOfWork unitOfWork,
		ICurrentUserService currentUserService)
	{
		this.auditLogRepository = auditLogRepository;
		this.unitOfWork = unitOfWork;
		this.currentUserService = currentUserService;
	}

	/// <summary>
	/// Handles the pipeline behavior for auditing
	/// </summary>
	public async Task<TResponse> Handle(
		TRequest request,
		CancellationToken cancellationToken,
		Func<Task<TResponse>> next)
	{
		// Check if the request should be audited
		Type requestType = request.GetType();
		var auditAttribute = requestType.GetCustomAttribute<AuditableAttribute>();
		if (auditAttribute == null)
		{
			return await next();
		}

		// Execute the command
		var response = await next();

		// Only audit successful operations (check if response is a successful Result)
		if (response is Result result && !result.IsSuccess)
		{
			return response;
		}

		// Extract entity ID if available from the response
		Guid? entityId = null;
		if (response is Result<Guid> guidResult)
		{
			entityId = guidResult.Value;
		}

		if (entityId == null && (auditAttribute.EntityType == "Connection" || auditAttribute.EntityType == "Redis"))
		{
			entityId = requestType.GetProperty("Id")?.GetValue(request) as Guid?
				?? requestType.GetProperty("ConnectionId")?.GetValue(request) as Guid?;
		}
		Guid? actorId = currentUserService.UserId;
		if (response is Result<LoginUserResponse> loginResult)
		{
			actorId = loginResult.Value.UserId;
			entityId = actorId;
		}

		// Create audit metadata
		string? outcome = response switch
		{
			Result<SeedDataResult> seed when seed.IsSuccess => $"Seeded {seed.Value.TotalKeysSeeded}; cancelled: {seed.Value.Cancelled}; unconfirmed changes: {seed.Value.MayHaveUnconfirmedChanges}.",
			Result<ImportRedisDataResponse> import when import.IsSuccess =>
				$"Imported {import.Value.ImportedCount}; skipped {import.Value.SkippedCount}; failed {import.Value.FailedCount}; cancelled: {import.Value.Cancelled}; unconfirmed changes: {import.Value.MayHaveUnconfirmedChanges}.",
			Result<DeleteMultipleKeysResponse> selected when selected.IsSuccess =>
				$"Deleted {selected.Value.DeletedCount} of {selected.Value.TotalRequested}; failed or missing: {selected.Value.FailedKeys.Count}.",
			Result<DeleteKeysByPatternResponse> pattern when pattern.IsSuccess => $"Deleted {pattern.Value.DeletedCount} matching keys.",
			Result<StreamDeleteKeysByPatternResult> stream when stream.IsSuccess =>
				$"Deleted {stream.Value.DeletedCount} matching keys; cancelled: {stream.Value.Cancelled}; failed: {stream.Value.Error is not null}.",
			Result<RedisBulkTtlResult> ttl when ttl.IsSuccess && request is BulkUpdateTtlCommand =>
				$"Updated TTL: {ttl.Value.UpdatedCount}; unchanged or missing: {ttl.Value.UnchangedCount}; failed: {ttl.Value.FailedKeys.Count}.",
			_ => null
		};
		var metadata = AuditMetadata.Create(
			ipAddress: currentUserService.IpAddress,
			userAgent: currentUserService.UserAgent,
			newValue: outcome);

		// Create audit log
		var auditLog = AuditLog.Create(
			userId: actorId,
			action: auditAttribute.Action,
			entityType: auditAttribute.EntityType ?? requestType.Name.Replace("Command", ""),
			entityId: entityId,
			metadata: metadata);

		auditLogRepository.Add(auditLog);
		await unitOfWork.SaveChangesAsync(request is StreamDeleteKeysByPatternCommand && cancellationToken.IsCancellationRequested
			? CancellationToken.None : cancellationToken);

		return response;
	}
}
