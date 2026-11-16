namespace RedisGUI.Domain.Connection;

/// <summary>Reads and restores Redis-native serialized key snapshots.</summary>
public interface IRedisSnapshotService
{
	Task<Result<RedisKeySnapshot>> CaptureAsync(RedisConnection connection, byte[] key, int database, CancellationToken cancellationToken = default);
	Task<Result<bool>> RestoreAsync(RedisConnection connection, byte[] key, byte[] payload, TimeSpan? ttl, bool overwrite,
		int database, CancellationToken cancellationToken = default);
	Task<Result<bool>> SetLegacyStringAsync(RedisConnection connection, string key, string value, TimeSpan? ttl, bool overwrite,
		int database, CancellationToken cancellationToken = default);
}

/// <summary>A Redis DUMP payload, its data type, and its remaining lifetime.</summary>
public sealed record RedisKeySnapshot(byte[] Payload, string Type, long? TtlMilliseconds);
