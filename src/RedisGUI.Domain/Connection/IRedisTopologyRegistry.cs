using System.Collections.Concurrent;

namespace RedisGUI.Domain.Connection;

/// <summary>Confirmed Redis deployment topology for a connection.</summary>
public enum RedisServerMode
{
	Unknown = 0,
	Standalone = 1,
	Cluster = 2,
	Sentinel = 3
}

/// <summary>Lock-free cache of confirmed Redis server topologies discovered from active connections.</summary>
public interface IRedisTopologyRegistry
{
	RedisServerMode GetMode(Guid connectionId);
	void SetMode(Guid connectionId, RedisServerMode mode);
	void Clear(Guid connectionId);
}

/// <summary>Thread-safe, lock-free implementation of <see cref="IRedisTopologyRegistry"/>.</summary>
public sealed class RedisTopologyRegistry : IRedisTopologyRegistry
{
	private readonly ConcurrentDictionary<Guid, RedisServerMode> modes = new();

	public RedisServerMode GetMode(Guid connectionId) =>
		modes.TryGetValue(connectionId, out var mode) ? mode : RedisServerMode.Unknown;

	public void SetMode(Guid connectionId, RedisServerMode mode)
	{
		if (mode == RedisServerMode.Unknown)
		{
			return;
		}
		modes[connectionId] = mode;
	}

	public void Clear(Guid connectionId) => modes.TryRemove(connectionId, out _);
}
