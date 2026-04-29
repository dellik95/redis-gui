namespace RedisGUI.Domain.Abstraction;

/// <summary>
/// Interface for password hashing and verification
/// </summary>
public interface IPasswordHasher
{
	/// <summary>
	/// Hashes a plain text password
	/// </summary>
	/// <param name="password">The plain text password</param>
	/// <returns>The hashed password</returns>
	string Hash(string password);

	/// <summary>
	/// Verifies a password against a hash
	/// </summary>
	/// <param name="password">The plain text password to verify</param>
	/// <param name="hashedPassword">The hashed password to verify against</param>
	/// <returns>True if the password matches the hash; otherwise, false</returns>
	bool Verify(string password, string hashedPassword);
}
