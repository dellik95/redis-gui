using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedisGUI.Infrastructure.Configuration;

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
		var sqlHash = ComputeHash(command.CommandText.Trim());

		var keyBuilder = new StringBuilder(this.config.KeyPrefix);
		keyBuilder.Append("|SQL_hash:").Append(sqlHash);
		keyBuilder.Append("|Context:").Append(cacheDbContextType);
		keyBuilder.Append("|Tables:").Append(tables);

		return keyBuilder.ToString();
	}

	private static string ComputeHash(string input, int hashLength = 15)
	{
		using var sha256 = SHA256.Create();
		var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
		return BitConverter.ToString(hashBytes).Replace("-", "").ToLower()[..hashLength];
	}
}
