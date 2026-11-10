using RedisGUI.Infrastructure.Redis.Common;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service responsible for executing raw Redis CLI commands, parsing command-line tokens, and formatting RESP output.
/// </summary>
public class RedisCliService(
	IConnectionPool connectionPool,
	ILogger<RedisCliService> logger) : IRedisCliService
{
	/// <inheritdoc />
	public async Task<Result<string>> ExecuteCliCommandAsync(
		RedisConnection connection,
		string rawCommand,
		int? database = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(rawCommand))
		{
			return Result.Failure<string>(new Error("Cli.EmptyCommand", "Command cannot be empty."));
		}

		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for CLI command: {Error}", multiplexerResult.Error.Message);
			return Result.Failure<string>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);

		string[] parts = CommandLineTokenizer.Tokenize(rawCommand);
		if (parts.Length == 0)
		{
			return Result.Failure<string>(new Error("Cli.EmptyCommand", "Command cannot be empty."));
		}

		string command = parts[0].ToUpperInvariant();
		object[] args = parts.Skip(1).Cast<object>().ToArray();

		try
		{
			RedisResult result = await db.ExecuteAsync(command, args);
			return Result.Success(RedisRespFormatter.Format(result));
		}
		catch (Exception ex)
		{
			return Result.Success($"(error) {ex.Message}");
		}
	}
}
