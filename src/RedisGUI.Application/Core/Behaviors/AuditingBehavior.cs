using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
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
		var auditAttribute = typeof(TRequest).GetCustomAttribute<AuditableAttribute>();
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

		// Create audit metadata
		var metadata = AuditMetadata.Create(
			ipAddress: currentUserService.IpAddress,
			userAgent: currentUserService.UserAgent);

		// Create audit log
		var auditLog = AuditLog.Create(
			userId: currentUserService.UserId,
			action: auditAttribute.Action,
			entityType: auditAttribute.EntityType ?? typeof(TRequest).Name.Replace("Command", ""),
			entityId: entityId,
			metadata: metadata);

		auditLogRepository.Add(auditLog);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return response;
	}
}
