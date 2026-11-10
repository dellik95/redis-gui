namespace RedisGUI.Domain.Audit;

/// <summary>
/// Represents actions that can be audited in the system
/// </summary>
public enum AuditAction
{
	/// <summary>
	/// Entity was created
	/// </summary>
	Create = 0,

	/// <summary>
	/// Entity was updated
	/// </summary>
	Update = 1,

	/// <summary>
	/// Entity was deleted
	/// </summary>
	Delete = 2,

	/// <summary>
	/// User logged in
	/// </summary>
	Login = 3,

	/// <summary>
	/// User logged out
	/// </summary>
	Logout = 4,

	/// <summary>
	/// Redis command executed
	/// </summary>
	RedisCommandExecuted = 5,

	/// <summary>
	/// Data exported
	/// </summary>
	DataExported = 6,

	/// <summary>
	/// Data imported
	/// </summary>
	DataImported = 7
}
