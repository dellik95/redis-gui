using Microsoft.Extensions.Logging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis transactions (MULTI/EXEC)
/// </summary>
public class RedisTransactionService : IRedisTransactionService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<RedisTransactionService> logger;

	public RedisTransactionService(
		IConnectionPool connectionPool,
		ILogger<RedisTransactionService> logger)
	{
		this.connectionPool = connectionPool;
		this.logger = logger;
	}

	/// <summary>
	/// Executes a Redis transaction (MULTI/EXEC)
	/// </summary>
	public async Task<Result<List<string>>> ExecuteTransactionAsync(
		RedisConnection connection,
		List<RedisOperation> operations,
		CancellationToken cancellationToken = default)
	{
		try
		{
			Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure<List<string>>(multiplexerResult.Error);
			}

			ConnectionMultiplexer multiplexer = multiplexerResult.Value;
			IDatabase database = multiplexer.GetDatabase(connection.DatabaseNumber.Value);
			ITransaction transaction = database.CreateTransaction();

			List<Task<StackExchange.Redis.RedisValue>> tasks = [];

			foreach (RedisOperation op in operations)
			{
				switch (op.Command.ToUpperInvariant())
				{
					case "SET":
						if (!string.IsNullOrEmpty(op.Value))
						{
							Task<bool> setTask = transaction.StringSetAsync(op.Key, op.Value);
							tasks.Add(setTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						}
						break;

					case "GET":
						Task<StackExchange.Redis.RedisValue> getTask = transaction.StringGetAsync(op.Key);
						tasks.Add(getTask);
						break;

					case "DEL":
						Task<bool> delTask = transaction.KeyDeleteAsync(op.Key);
						tasks.Add(delTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						break;

					case "INCR":
						Task<long> incrTask = transaction.StringIncrementAsync(op.Key);
						tasks.Add(incrTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						break;

					case "DECR":
						Task<long> decrTask = transaction.StringDecrementAsync(op.Key);
						tasks.Add(decrTask.ContinueWith(t => new StackExchange.Redis.RedisValue(t.Result.ToString())));
						break;

					default:
						logger.LogWarning("Unsupported transaction command: {Command}", op.Command);
						break;
				}
			}

			bool committed = await transaction.ExecuteAsync();

			if (!committed)
			{
				return Result.Failure<List<string>>(DomainErrors.Redis.TransactionFailed);
			}

			await Task.WhenAll(tasks);

			List<string> results = tasks.Select(t => t.Result.ToString()).ToList();
			return Result.Success(results);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error executing transaction");
			return Result.Failure<List<string>>(DomainErrors.Redis.TransactionFailed);
		}
	}
}
