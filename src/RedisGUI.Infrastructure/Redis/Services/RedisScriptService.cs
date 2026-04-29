using Microsoft.Extensions.Logging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis Lua script execution
/// </summary>
public class RedisScriptService : IRedisScriptService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<RedisScriptService> logger;

	public RedisScriptService(
		IConnectionPool connectionPool,
		ILogger<RedisScriptService> logger)
	{
		this.connectionPool = connectionPool;
		this.logger = logger;
	}

	/// <summary>
	/// Executes a Lua script in Redis using EVAL with SHA-based caching
	/// </summary>
	public async Task<Result<(string Result, string ScriptHash)>> ExecuteLuaScriptAsync(
		RedisConnection connection,
		string script,
		string[] keys,
		string[] arguments,
		CancellationToken cancellationToken = default)
	{
		try
		{
			Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure<(string, string)>(multiplexerResult.Error);
			}

			ConnectionMultiplexer multiplexer = multiplexerResult.Value;
			IDatabase database = multiplexer.GetDatabase(connection.DatabaseNumber.Value);

			// Generate SHA1 hash for the script
			string scriptHash = ComputeSha1(script);

			// Convert keys and arguments to RedisKey and RedisValue
			RedisKey[] redisKeys = keys.Select(k => (RedisKey)k).ToArray();
			StackExchange.Redis.RedisValue[] redisValues = arguments.Select(a => (StackExchange.Redis.RedisValue)a).ToArray();

			StackExchange.Redis.RedisValue result;

			try
			{
				// Try to execute using cached script hash (EVALSHA)
				RedisResult evalResult = await database.ScriptEvaluateAsync(scriptHash, redisKeys, redisValues);
				result = ConvertRedisResultToValue(evalResult);
			}
			catch (RedisServerException ex) when (ex.Message.Contains("NOSCRIPT"))
			{
				// Script not cached, use EVAL
				RedisResult evalResult = await database.ScriptEvaluateAsync(script, redisKeys, redisValues);
				result = ConvertRedisResultToValue(evalResult);
			}

			return Result.Success((result.ToString(), scriptHash));
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error executing Lua script");
			return Result.Failure<(string, string)>(DomainErrors.Redis.ScriptFailed);
		}
	}

	#region Private Helper Methods

	private static string ComputeSha1(string input)
	{
		byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	private static StackExchange.Redis.RedisValue ConvertRedisResultToValue(RedisResult result)
	{
		// RedisResult can be converted to RedisValue
		if (result.IsNull)
		{
			return StackExchange.Redis.RedisValue.Null;
		}

		return (StackExchange.Redis.RedisValue)result;
	}

	#endregion
}
