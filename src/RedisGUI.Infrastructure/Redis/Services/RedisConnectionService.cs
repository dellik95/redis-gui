using Microsoft.Extensions.Logging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Service for managing Redis connection health and testing
/// </summary>
public class RedisConnectionService : IRedisConnectionService
{
	private readonly IConnectionPool connectionPool;
	private readonly ILogger<RedisConnectionService> logger;

	public RedisConnectionService(
		IConnectionPool connectionPool,
		ILogger<RedisConnectionService> logger)
	{
		this.connectionPool = connectionPool;
		this.logger = logger;
	}

	/// <summary>
	/// Verifies if a Redis connection can be established
	/// </summary>
	public async Task<Result> CheckConnectionAsync(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);

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
