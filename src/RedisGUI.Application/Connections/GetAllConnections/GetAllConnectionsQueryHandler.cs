using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetAllConnections
{
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
}
