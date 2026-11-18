namespace RedisGUI.Domain.Audit;

/// <summary>Formats a feature-owned summary for audit logging.</summary>
public interface IAuditOutcome
{
	string? FormatAuditOutcome();
}

/// <summary>Identifies the actor and target entity from an authentication result.</summary>
public interface IAuditActorResult
{
	Guid ActorUserId { get; }
	Guid? AuditEntityId { get; }
}

/// <summary>Supplies audit target metadata without reflection or feature coupling.</summary>
public interface IAuditEntityRequest
{
	Guid? AuditEntityId { get; }
	bool PersistAuditOnCancellation => false;
}
