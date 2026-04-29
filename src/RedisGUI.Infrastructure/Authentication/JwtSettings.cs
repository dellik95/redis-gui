using System.ComponentModel.DataAnnotations;

namespace RedisGUI.Infrastructure.Authentication;

/// <summary>
/// Configuration settings for JWT token generation
/// </summary>
public sealed class JwtSettings
{
	/// <summary>
	/// The configuration section name in appsettings.json
	/// </summary>
	public const string SectionName = "JwtSettings";

	/// <summary>
	/// The secret key used for signing tokens (minimum 32 characters)
	/// </summary>
	[Required]
	[MinLength(32)]
	public string SecretKey { get; init; } = string.Empty;

	/// <summary>
	/// The token issuer (typically the application name)
	/// </summary>
	[Required]
	public string Issuer { get; init; } = string.Empty;

	/// <summary>
	/// The intended token audience (typically the client application)
	/// </summary>
	[Required]
	public string Audience { get; init; } = string.Empty;

	/// <summary>
	/// Token expiration time in minutes (default: 60 minutes)
	/// </summary>
	public int ExpirationMinutes { get; init; } = 60;
}
