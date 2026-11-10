using RedisGUI.Domain.Abstraction;

namespace RedisGUI.Domain.Audit;

/// <summary>
/// Aggregate root representing an audit log entry
/// </summary>
public sealed class AuditLog : Entity, IAggregateRoot
{
	private AuditLog() { } // EF Core

	private AuditLog(
		Guid id,
		Guid? userId,
		AuditAction action,
		string entityType,
		Guid? entityId,
		AuditMetadata metadata) : base(id)
	{
		UserId = userId;
		Action = action;
		EntityType = entityType;
		EntityId = entityId;
		Metadata = metadata;
		Timestamp = DateTime.UtcNow;
	}

	/// <summary>
	/// Gets the ID of the user who performed the action (null for anonymous/system actions)
	/// </summary>
	public Guid? UserId { get; private set; }

	/// <summary>
	/// Gets the action that was performed
	/// </summary>
	public AuditAction Action { get; private set; }

	/// <summary>
	/// Gets the type of entity that was affected
	/// </summary>
	public string EntityType { get; private set; } = null!;

	/// <summary>
	/// Gets the ID of the entity that was affected (null for general actions)
	/// </summary>
	public Guid? EntityId { get; private set; }

	/// <summary>
	/// Gets the audit metadata (IP address, user agent, old/new values)
	/// </summary>
	public AuditMetadata Metadata { get; private set; } = null!;

	/// <summary>
	/// Gets the timestamp when the action occurred
	/// </summary>
	public DateTime Timestamp { get; private set; }

	/// <summary>
	/// Creates a new audit log entry
	/// </summary>
	/// <param name="userId">The ID of the user who performed the action</param>
	/// <param name="action">The action that was performed</param>
	/// <param name="entityType">The type of entity affected</param>
	/// <param name="entityId">The ID of the entity affected</param>
	/// <param name="metadata">Additional metadata about the action</param>
	/// <returns>A new AuditLog instance</returns>
	public static AuditLog Create(
		Guid? userId,
		AuditAction action,
		string entityType,
		Guid? entityId,
		AuditMetadata metadata)
	{
		return new AuditLog(
			Guid.NewGuid(),
			userId,
			action,
			entityType,
			entityId,
			metadata);
	}
}
