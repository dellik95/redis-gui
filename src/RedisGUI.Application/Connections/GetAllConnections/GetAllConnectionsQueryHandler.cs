using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Connections.GetAllConnections
{
	internal sealed class GetAllConnectionsQueryHandler : IQueryHandler<GetAllConnectionsQuery, IEnumerable<GetConnectionResponse>>
	{
		private readonly IRedisConnectionRepository redisConnectionRepository;
		private readonly IPasswordDecryptor passwordDecryptor;

		public GetAllConnectionsQueryHandler(IRedisConnectionRepository redisConnectionRepository, IPasswordDecryptor passwordDecryptor)
		{
			this.redisConnectionRepository = redisConnectionRepository;
			this.passwordDecryptor = passwordDecryptor;
		}


		public async Task<Result<IEnumerable<GetConnectionResponse>>> Handle(GetAllConnectionsQuery request, CancellationToken cancellationToken)
		{
			var connectionsResult = await this.redisConnectionRepository.GetAsync(null, false, request.PageNumber, request.PageSize);

			return connectionsResult.Map(connections => connections.Select(c => GetConnectionResponse.FromRedisConnection(c, passwordDecryptor)));
		}
	}
}