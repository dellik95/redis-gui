namespace RedisGUI.Domain.Users;

/// <summary>
/// Enumeration of user roles in the system
/// </summary>
public enum UserRole
{
	/// <summary>
	/// Standard user with basic access
	/// </summary>
	User = 0,

	/// <summary>
	/// Administrator with full access
	/// </summary>
	Admin = 1,

	/// <summary>
	/// Read-only user without write permissions
	/// </summary>
	ReadOnly = 2
}
