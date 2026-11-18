using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Messaging;

internal static class MessagingConnectionAccess
{
	internal static async Task<Result<RedisConnection>> FindAsync(Guid id, IRedisConnectionRepository repository,
		IConnectionAccessService access, CancellationToken cancellationToken)
	{
		var result = await repository.GetConnectionByIdAsync(id, cancellationToken);
		return await result.BindAsync(async connection => await access.CanAccessAsync(connection)
			? Result.Success(connection)
			: Result.Failure<RedisConnection>(new Error("Connection.Forbidden", "Access to the connection is denied.")));
	}
}
