using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Mock implementation of IRedisConnectionService for testing
/// </summary>
public sealed class MockRedisConnectionService : IRedisConnectionService
{
	private readonly ILogger<MockRedisConnectionService>? _logger;

	public MockRedisConnectionService(ILogger<MockRedisConnectionService>? logger = null)
	{
		_logger = logger;
	}

	public Task<Result> CheckConnectionAsync(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		_logger?.LogDebug("Checking mock Redis connection for {ConnectionId}", connection.Id);
		// Mock always returns success
		return Task.FromResult(Result.Success());
	}
}
