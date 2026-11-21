using FluentResults;
using RedisGUI.Domain.Errors;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service for managing Redis connection health, topology detection, and testing.
/// </summary>
public class RedisConnectionService(
	IRedisConnectionManager connectionManager,
	ILogger<RedisConnectionService> logger,
	IRedisTopologyRegistry? topology = null) : IRedisConnectionService
{
	/// <summary>
	/// Verifies if a Redis connection can be established.
	/// </summary>
	public async Task<Result> CheckConnectionAsync(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);

		if (multiplexerResult.IsFailed)
		{
			logger.LogError("Failed to connect: {Error}", multiplexerResult.GetDomainError().Message);
			return Result.Fail(multiplexerResult.Errors);
		}

		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		topology?.SetMode(connection.Id, multiplexer.DetectServerMode());

		int targetDb = multiplexer.IsCluster() ? 0 : connection.DatabaseNumber.Value;
		TimeSpan result = await multiplexer.GetDatabase(targetDb).PingAsync().WaitAsync(cancellationToken);

		return result.CompareTo(TimeSpan.Zero) > 0
			? Result.Ok()
			: Result.Fail(DomainErrors.Connection.ConnectionNotEstablished);
	}
}
