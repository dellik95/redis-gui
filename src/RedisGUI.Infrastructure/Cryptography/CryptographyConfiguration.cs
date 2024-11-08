using System.ComponentModel.DataAnnotations;

namespace RedisGUI.Infrastructure.Cryptography;

/// <summary>
/// Configuration class for cryptography settings used in the application.
/// </summary>
public sealed class CryptographyConfiguration
{
	/// <summary>
	/// The configuration key used to identify cryptography settings in configuration files.
	/// </summary>
	public const string Key = "Cryptography";

	/// <summary>
	/// Gets or sets the security key used for encryption and decryption operations.
	/// This property is required and must not be empty.
	/// </summary>
	[Required] public string SecurityKey { get; set; }
}
