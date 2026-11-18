using System.Linq;
using System.Text;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>Resolves servers, topology mode, cluster hash slots, and binary-safe Redis keys.</summary>
internal static class RedisServerResolver
{
	internal static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	internal static async Task<Result<ConnectionMultiplexer>> GetConnectionAsync(
		this IRedisConnectionManager connectionManager,
		RedisConnection connection,
		IRedisTopologyRegistry? topology,
		CancellationToken cancellationToken)
	{
		var result = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (result.IsSuccess)
		{
			topology?.SetMode(connection.Id, result.Value.DetectServerMode());
		}
		return result;
	}

	internal static async Task<Result<(ConnectionMultiplexer Multiplexer, IDatabase Database, int DatabaseIndex)>> GetValidatedDatabaseAsync(
		this IRedisConnectionManager connectionManager,
		RedisConnection connection,
		int? database,
		IRedisTopologyRegistry? topology,
		CancellationToken cancellationToken)
	{
		var multiplexerResult = await connectionManager.GetConnectionAsync(connection, topology, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<(ConnectionMultiplexer, IDatabase, int)>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailure)
		{
			return Result.Failure<(ConnectionMultiplexer, IDatabase, int)>(dbValidation.Error);
		}

		return Result.Success((multiplexerResult.Value, multiplexerResult.Value.GetDatabase(targetDb), targetDb));
	}

	internal static IServer GetConnectedServer(this ConnectionMultiplexer multiplexer, RedisConnection connection)
	{
		try
		{
			var direct = multiplexer.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);
			if (direct.IsConnected)
			{
				return direct;
			}
		}
		catch (ArgumentException)
		{
		}

		var servers = multiplexer.GetServers().Where(server => server.IsConnected).ToArray();
		return servers.FirstOrDefault(server => !server.IsReplica)
			?? servers.FirstOrDefault()
			?? throw new InvalidOperationException("No connected Redis server is available.");
	}

	internal static RedisServerMode DetectServerMode(this ConnectionMultiplexer multiplexer)
	{
		var servers = multiplexer.GetServers().Where(s => s.IsConnected).ToArray();
		if (servers.Length == 0)
		{
			servers = multiplexer.GetServers();
		}

		if (servers.Any(s => s.ServerType == ServerType.Cluster))
		{
			return RedisServerMode.Cluster;
		}
		if (servers.Any(s => s.ServerType == ServerType.Sentinel))
		{
			return RedisServerMode.Sentinel;
		}
		if (servers.Any(s => s.IsConnected))
		{
			return RedisServerMode.Standalone;
		}

		return RedisServerMode.Unknown;
	}

	internal static bool IsCluster(this ConnectionMultiplexer multiplexer) =>
		multiplexer.DetectServerMode() == RedisServerMode.Cluster;

	internal static Result ValidateDatabase(this ConnectionMultiplexer multiplexer, int database)
	{
		if (database is < 0 or > 15)
		{
			return Result.Failure(new Error("Redis.InvalidDatabase", "Redis database index must be between 0 and 15."));
		}

		if (multiplexer.IsCluster() && database != 0)
		{
			return Result.Failure(new Error("Redis.InvalidDatabase", "Redis Cluster supports database 0 only."));
		}

		return Result.Success();
	}

	internal static IServer[] GetConnectedPrimaryServers(this ConnectionMultiplexer multiplexer) =>
		multiplexer.GetServers()
			.Where(s => s.IsConnected && s.ServerType != ServerType.Sentinel && !s.IsReplica)
			.ToArray();

	internal static Result<RedisKey> ResolveKey(string key, string? keyBase64)
	{
		if (!string.IsNullOrEmpty(keyBase64))
		{
			try
			{
				byte[] raw = Convert.FromBase64String(keyBase64);
				if (raw.Length == 0)
				{
					return Result.Failure<RedisKey>(new Error("RedisValue.EmptyKey", "Redis key cannot be empty."));
				}
				return Result.Success((RedisKey)raw);
			}
			catch (FormatException)
			{
				return Result.Failure<RedisKey>(new Error("RedisValue.InvalidKeyEncoding", "KeyBase64 must be valid Base64."));
			}
		}

		if (string.IsNullOrEmpty(key))
		{
			return Result.Failure<RedisKey>(new Error("RedisValue.EmptyKey", "Redis key cannot be empty."));
		}

		return Result.Success((RedisKey)key);
	}

	internal static bool IsLosslessUtf8(byte[] rawKey)
	{
		try
		{
			_ = StrictUtf8.GetString(rawKey);
			return true;
		}
		catch (DecoderFallbackException)
		{
			return false;
		}
	}
}
