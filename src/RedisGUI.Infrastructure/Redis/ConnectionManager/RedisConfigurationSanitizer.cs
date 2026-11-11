using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.ConnectionManager;

/// <summary>
/// Utility for sanitizing Redis connection strings and extracting stable identifiers without exposing secrets.
/// </summary>
public static partial class RedisConfigurationSanitizer
{
	[GeneratedRegex(@"(password\s*=\s*)([^,;]+)", RegexOptions.IgnoreCase)]
	private static partial Regex PasswordRegex();

	/// <summary>
	/// Replaces sensitive credentials such as passwords with asterisks for safe logging.
	/// </summary>
	public static string Sanitize(string? configuration)
	{
		if (string.IsNullOrWhiteSpace(configuration))
		{
			return string.Empty;
		}

		return PasswordRegex().Replace(configuration, "$1******");
	}

	/// <summary>
	/// Returns a stable identifier for a Redis server.
	/// If an explicit key is provided, returns it; otherwise extracts normalized endpoints.
	/// </summary>
	public static string GetStableKey(string? explicitKey, string configuration)
	{
		if (!string.IsNullOrWhiteSpace(explicitKey))
		{
			return explicitKey.Trim();
		}

		try
		{
			ConfigurationOptions options = ConfigurationOptions.Parse(configuration);
			var endpoints = options.EndPoints
				.Select(e => e switch
				{
					System.Net.DnsEndPoint dns => $"{dns.Host}:{dns.Port}",
					System.Net.IPEndPoint ip => $"{ip.Address}:{ip.Port}",
					_ => e.ToString() ?? string.Empty
				})
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.OrderBy(e => e, StringComparer.OrdinalIgnoreCase);

			string joined = string.Join(",", endpoints);
			if (!string.IsNullOrWhiteSpace(joined))
			{
				return !string.IsNullOrWhiteSpace(options.User)
					? $"{options.User}@{joined}"
					: joined;
			}
		}
		catch
		{
			// Fallback to sanitized configuration string if parsing fails
		}

		return Sanitize(configuration);
	}

	/// <summary>
	/// Computes a deterministic SHA256 hash fingerprint of the configuration to detect changes.
	/// </summary>
	public static string ComputeFingerprint(string configuration)
	{
		if (string.IsNullOrWhiteSpace(configuration))
		{
			return string.Empty;
		}

		byte[] bytes = Encoding.UTF8.GetBytes(configuration);
		byte[] hash = SHA256.HashData(bytes);
		return Convert.ToHexString(hash);
	}
}
