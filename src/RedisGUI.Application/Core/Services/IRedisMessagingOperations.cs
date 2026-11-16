using RedisGUI.Domain.Connection;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Services;

/// <summary>Messaging operations required by application requests.</summary>
public interface IRedisMessagingOperations
{
	Task<object> GetStreamAsync(RedisConnection connection, string key, CancellationToken cancellationToken);
	Task<object> GetChannelAsync(RedisConnection connection, string channel, CancellationToken cancellationToken);
	Task<long> PublishAsync(RedisConnection connection, string channel, string message, CancellationToken cancellationToken);
}
