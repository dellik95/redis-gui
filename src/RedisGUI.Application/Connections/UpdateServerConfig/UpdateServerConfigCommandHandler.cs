using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.UpdateServerConfig;

public sealed class UpdateServerConfigCommandHandler : ICommandHandler<UpdateServerConfigCommand, RedisSetConfigResponseDto>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisDiagnosticsService diagnosticsService;

	public UpdateServerConfigCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisDiagnosticsService diagnosticsService)
	{
		this.connectionRepository = connectionRepository;
		this.diagnosticsService = diagnosticsService;
	}

	public async Task<Result<RedisSetConfigResponseDto>> Handle(UpdateServerConfigCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisSetConfigResponseDto>(connectionResult.Error);
		}

		var connection = connectionResult.Value;
		if (connection == null)
		{
			return Result.Failure<RedisSetConfigResponseDto>(DomainErrors.Connection.NotFound);
		}

		if (connection.IsReadOnly)
		{
			return Result.Failure<RedisSetConfigResponseDto>(
				new Error("Connection.ReadOnly", "This connection is in read-only safe mode. Configuration changes are disabled."));
		}

		return await diagnosticsService.SetServerConfigAsync(
			connection,
			request.ParameterName,
			request.ParameterValue,
			cancellationToken);
	}
}
