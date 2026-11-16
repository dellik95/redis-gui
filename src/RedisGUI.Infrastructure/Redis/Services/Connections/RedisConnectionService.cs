using RedisGUI.Domain.Errors;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service for managing Redis connection health and testing
/// </summary>
public class RedisConnectionService : IRedisConnectionService
{
	private readonly IRedisConnectionManager connectionManager;
	private readonly ILogger<RedisConnectionService> logger;

	public RedisConnectionService(
		IRedisConnectionManager connectionManager,
		ILogger<RedisConnectionService> logger)
	{
		this.connectionManager = connectionManager;
		this.logger = logger;
	}

	/// <summary>
	/// Verifies if a Redis connection can be established
	/// </summary>
	public async Task<Result> CheckConnectionAsync(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);

		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to connect: {Error}", multiplexerResult.Error.Message);
			return Result.Failure(multiplexerResult.Error);
		}

		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		TimeSpan result = multiplexer.GetDatabase(connection.DatabaseNumber.Value).Ping();

		return result.CompareTo(TimeSpan.Zero) > 0
			? Result.Success()
			: Result.Failure(DomainErrors.Connection.ConnectionNotEstablished);
	}
}
