namespace RedisGUI.Infrastructure.Authorization;

/// <summary>
/// Policy names for authorization
/// </summary>
public static class Policies
{
	/// <summary>
	/// Requires Admin role
	/// </summary>
	public const string AdminOnly = "AdminOnly";

	/// <summary>
	/// Requires Admin or User role (excludes ReadOnly)
	/// </summary>
	public const string WriteAccess = "WriteAccess";

	/// <summary>
	/// Requires any authenticated user
	/// </summary>
	public const string Authenticated = "Authenticated";
}
