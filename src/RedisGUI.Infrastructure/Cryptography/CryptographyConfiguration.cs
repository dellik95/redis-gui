using System.ComponentModel.DataAnnotations;

namespace RedisGUI.Infrastructure.Cryptography;

public sealed class CryptographyConfiguration
{
	public const string Key = "Cryptography";

	[Required]
	public string SecurityKey { get; set; }
}