using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.ScanRedisKeys;

internal sealed class ScanRedisKeysQueryHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IRedisKeyService keys)
	: IQueryHandler<ScanRedisKeysQuery, RedisKeyScanPage>
{
	public async Task<Result<RedisKeyScanPage>> Handle(ScanRedisKeysQuery request, CancellationToken cancellationToken)
	{
		return await (await repository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken))
			.BindAsync(async connection => await access.CanAccessAsync(connection)
				? await keys.ScanKeysAsync(connection, request.Pattern, request.Database ?? connection.DatabaseNumber.Value,
					request.Cursor, request.Count, cancellationToken)
				: Result.Failure<RedisKeyScanPage>(new Error("Connection.Forbidden", "Access to the connection is denied.")));
	}
}
