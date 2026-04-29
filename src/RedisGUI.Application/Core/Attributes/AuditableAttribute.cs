using RedisGUI.Domain.Audit;
using System;

namespace RedisGUI.Application.Core.Behaviors;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AuditableAttribute : Attribute
{
	/// <summary>
	/// Gets the audit action type
	/// </summary>
	public AuditAction Action { get; }

	/// <summary>
	/// Gets the entity type being audited (optional, defaults to command name)
	/// </summary>
	public string? EntityType { get; }

	/// <summary>
	/// Initializes a new instance of the AuditableAttribute
	/// </summary>
	/// <param name="action">The audit action type</param>
	/// <param name="entityType">Optional entity type name</param>
	public AuditableAttribute(AuditAction action, string? entityType = null)
	{
		Action = action;
		EntityType = entityType;
	}
}
