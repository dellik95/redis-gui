using RedisGUI.Domain.Users;

namespace RedisGUI.Domain.Abstraction;

/// <summary>
/// Generates JWT tokens for authenticated users
/// </summary>
public interface IJwtTokenGenerator
{
	/// <summary>
	/// Generates a JWT token for the specified user
	/// </summary>
	/// <param name="user">The user to generate a token for</param>
	/// <returns>A JWT token string containing user claims</returns>
	string GenerateToken(User user);
}
