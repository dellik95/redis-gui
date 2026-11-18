using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Services;

/// <summary>Checks whether the current user may access a connection.</summary>
public interface IConnectionAccessService
{
	Task<bool> CanAccessAsync(RedisConnection connection);
}

/// <summary>Shared authorization and read-only guard extensions for Redis connection lookups.</summary>
public static class ConnectionAccessExtensions
{
	public static async Task<Result<RedisConnection>> GetAuthorizedConnectionAsync(
		this IRedisConnectionRepository connectionRepository,
		Guid connectionId,
		IConnectionAccessService? access,
		CancellationToken cancellationToken,
		bool requireWritable = false,
		Error? readOnlyError = null)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(connectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisConnection>(connectionResult.Error);
		}

		var connection = connectionResult.Value;
		if (connection is null)
		{
			return Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound);
		}

		if (access is not null && !await access.CanAccessAsync(connection))
		{
			return Result.Failure<RedisConnection>(new Error("Connection.Forbidden", "Access to the connection is denied."));
		}

		if (requireWritable && connection.IsReadOnly)
		{
			return Result.Failure<RedisConnection>(readOnlyError ?? DomainErrors.Connection.ReadOnlyMode);
		}

		return Result.Success(connection);
	}
}
