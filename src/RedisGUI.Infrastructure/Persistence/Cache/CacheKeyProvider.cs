using RedisGUI.Infrastructure.Configuration;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RedisGUI.Infrastructure.Persistence.Cache;

/// <summary>
/// Provides functionality to generate cache keys for database queries
/// </summary>
public class CacheKeyProvider : ICacheKeyProvider
{
	private readonly ILogger<CacheKeyProvider> keyProviderLogger;
	private readonly CacheConfiguration config;

	/// <summary>
	/// Initializes a new instance of the CacheKeyProvider
	/// </summary>
	/// <param name="options">Cache configuration options</param>
	/// <param name="keyProviderLogger">Logger for the cache key provider</param>
	public CacheKeyProvider(IOptions<CacheConfiguration> options, ILogger<CacheKeyProvider> keyProviderLogger)
	{
		this.keyProviderLogger = keyProviderLogger ?? throw new ArgumentNullException(nameof(keyProviderLogger));
		this.config = options.Value;
	}

	/// <inheritdoc />
	public string GetCacheKey(DbCommand command, DbContext context, SortedSet<string> tableNames)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(tableNames);

		var cacheDbContextType = context.GetType();
		var tables = string.Join(";", tableNames);
		var sqlHash = ComputeHash(BuildCommandFingerprint(command));

		var keyBuilder = new StringBuilder(this.config.KeyPrefix);
		keyBuilder.Append("|SQL_hash:").Append(sqlHash);
		keyBuilder.Append("|Context:").Append(cacheDbContextType);
		keyBuilder.Append("|Tables:").Append(tables);

		return keyBuilder.ToString();
	}

	private static string BuildCommandFingerprint(DbCommand command)
	{
		var commandText = command.CommandText.Trim();
		if (command.Parameters.Count == 0)
		{
			return commandText;
		}

		var builder = new StringBuilder(commandText.Length + command.Parameters.Count * 32);
		builder.Append(commandText.Length).Append(':').Append(commandText);

		for (int i = 0; i < command.Parameters.Count; i++)
		{
			DbParameter parameter = command.Parameters[i];
			string formattedValue = FormatParameterValue(parameter.Value);
			builder.Append('|')
				.Append(parameter.ParameterName)
				.Append('#').Append((int)parameter.DbType)
				.Append('#').Append((int)parameter.Direction)
				.Append('#').Append(formattedValue.Length)
				.Append(':').Append(formattedValue);
		}

		return builder.ToString();
	}

	private static string FormatParameterValue(object? value) => value switch
	{
		null or DBNull => "<null>",
		byte[] bytes => "hex:" + Convert.ToHexString(bytes),
		DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture) + ":" + (int)dt.Kind,
		DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
		IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString() ?? string.Empty
	};

	private static string ComputeHash(string input, int hashLength = 32)
	{
		var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		return Convert.ToHexStringLower(hashBytes)[..hashLength];
	}
}
