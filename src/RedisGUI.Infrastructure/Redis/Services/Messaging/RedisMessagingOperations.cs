using RedisGUI.Application.Core.Services;

namespace RedisGUI.Infrastructure.Redis.Services;

internal sealed class RedisMessagingOperations(RedisMessagingService messaging, RedisMessagingMetricsCollector metrics) : IRedisMessagingOperations
{
	public Task<object> GetStreamAsync(RedisConnection connection, string key, CancellationToken cancellationToken) =>
		messaging.StreamAsync(connection, key, cancellationToken);

	public Task<object> GetChannelAsync(RedisConnection connection, string channel, CancellationToken cancellationToken) =>
		metrics.CollectAsync(connection, channel, cancellationToken);

	public Task<long> PublishAsync(RedisConnection connection, string channel, string message, CancellationToken cancellationToken) =>
		messaging.PublishAsync(connection, channel, message, cancellationToken);
}
