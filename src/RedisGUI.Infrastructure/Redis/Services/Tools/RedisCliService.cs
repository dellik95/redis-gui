using FluentResults;
using RedisGUI.Infrastructure.Redis.Common;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service responsible for executing raw Redis CLI commands, parsing command-line tokens, and formatting RESP output.
/// </summary>
public class RedisCliService(
	IRedisConnectionManager connectionManager,
	ILogger<RedisCliService> logger,
	IRedisTopologyRegistry? topology = null) : IRedisCliService
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
			return Result.Fail<string>(new DomainError("Cli.EmptyCommand", "Command cannot be empty."));
		}

		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			logger.LogError("Failed to get connection for CLI command: {Error}", multiplexerResult.GetDomainError().Message);
			return Result.Fail<string>(multiplexerResult.Errors);
		}

		var multiplexer = multiplexerResult.Value;
		topology?.SetMode(connection.Id, multiplexer.DetectServerMode());

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexer.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed)
		{
			return Result.Ok($"(error) {dbValidation.GetDomainError().Message}");
		}

		IDatabase db = multiplexer.GetDatabase(targetDb);

		string[] parts = CommandLineTokenizer.Tokenize(rawCommand);
		if (parts.Length == 0)
		{
			return Result.Fail<string>(new DomainError("Cli.EmptyCommand", "Command cannot be empty."));
		}

		string command = parts[0].ToUpperInvariant();
		object[] args = parts.Skip(1).Cast<object>().ToArray();

		try
		{
			RedisResult result = await db.ExecuteAsync(command, args).WaitAsync(cancellationToken);
			return Result.Ok(RedisRespFormatter.Format(result));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Result.Ok($"(error) {ex.Message}");
		}
	}
}
