using DispatchWeave;
using FluentResults;
using RedisGUI.Application.Core.Attributes;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Behaviors;

/// <summary>
/// Pipeline behavior that creates audit logs for commands marked with <see cref="AuditableAttribute"/>.
/// Uses domain audit contracts so feature-specific outcome formatting stays decoupled.
/// </summary>
public sealed class AuditingBehavior<TRequest, TResponse>(
	IAuditLogRepository auditLogRepository,
	IUnitOfWork unitOfWork,
	ICurrentUserService currentUserService) : IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken = default)
	{
		Type requestType = request.GetType();
		var auditAttribute = requestType.GetCustomAttribute<AuditableAttribute>();
		if (auditAttribute is null)
		{
			return await next();
		}

		var response = await next();

		if (response is IResultBase { IsFailed: true })
		{
			return response;
		}

		object? resultValue = response switch
		{
			IResult<Guid> guid => guid.Value,
			IResult<IAuditActorResult> actor => actor.Value,
			IResult<IAuditOutcome> outcomeResult => outcomeResult.Value,
			_ => null
		};

		Guid? entityId = resultValue is Guid id ? id : null;
		Guid? actorId = currentUserService.UserId;

		if (resultValue is IAuditActorResult actorResult)
		{
			actorId = actorResult.ActorUserId;
			entityId = actorResult.AuditEntityId;
		}

		if (entityId is null && request is IAuditEntityRequest entityRequest)
		{
			entityId = entityRequest.AuditEntityId;
		}

		string? outcome = (resultValue as IAuditOutcome)?.FormatAuditOutcome();
		var metadata = AuditMetadata.Create(
			ipAddress: currentUserService.IpAddress,
			userAgent: currentUserService.UserAgent,
			newValue: outcome);

		var auditLog = AuditLog.Create(
			userId: actorId,
			action: auditAttribute.Action,
			entityType: auditAttribute.EntityType ?? requestType.Name.Replace("Command", ""),
			entityId: entityId,
			metadata: metadata);

		auditLogRepository.Add(auditLog);

		bool persistOnCancellation = request is IAuditEntityRequest { PersistAuditOnCancellation: true };
		await unitOfWork.SaveChangesAsync(
			persistOnCancellation && cancellationToken.IsCancellationRequested
				? CancellationToken.None
				: cancellationToken);

		return response;
	}
}
