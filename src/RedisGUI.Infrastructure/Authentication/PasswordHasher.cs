namespace RedisGUI.Infrastructure.Authentication;

/// <summary>
/// Hashes and verifies passwords using BCrypt algorithm
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
	private const int WorkFactor = 11; // BCrypt work factor (2^11 iterations)

	/// <summary>
	/// Hashes a plain text password using BCrypt
	/// </summary>
	public string Hash(string password)
	{
		return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
	}

	/// <summary>
	/// Verifies a plain text password against a BCrypt hash
	/// </summary>
	public bool Verify(string password, string hash)
	{
		try
		{
			return BCrypt.Net.BCrypt.Verify(password, hash);
		}
		catch
		{
			// If hash is invalid, return false instead of throwing
			return false;
		}
	}
}
