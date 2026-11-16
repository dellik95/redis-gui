using System.Collections.Generic;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Shared.Mediator;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetAllConnections;

/// <summary>
///    Query to retrieve a Redis connections.
/// </summary>
/// <param name="PageNumber">Page number</param>
/// <param name="PageSize">Items count per page</param>
public record GetAllConnectionsQuery(int PageNumber, int PageSize) : IQuery<IEnumerable<GetConnectionResponse>>;

internal sealed class GetAllConnectionsQueryHandler : IQueryHandler<GetAllConnectionsQuery, IEnumerable<GetConnectionResponse>>
{
	private readonly IRedisConnectionRepository redisConnectionRepository;
	private readonly IPasswordDecrypt passwordDecrypt;

	public GetAllConnectionsQueryHandler(IRedisConnectionRepository redisConnectionRepository, IPasswordDecrypt passwordDecrypt)
	{
		this.redisConnectionRepository = redisConnectionRepository;
		this.passwordDecrypt = passwordDecrypt;
	}


	public async Task<Result<IEnumerable<GetConnectionResponse>>> Handle(GetAllConnectionsQuery request, CancellationToken cancellationToken)
	{
		var connectionsResult = await this.redisConnectionRepository.GetAsync(null, false, request.PageNumber, request.PageSize);

		return connectionsResult.Map(pagedConnections => pagedConnections.Items.Select(c => GetConnectionResponse.FromRedisConnection(c, passwordDecrypt)));
	}
}
