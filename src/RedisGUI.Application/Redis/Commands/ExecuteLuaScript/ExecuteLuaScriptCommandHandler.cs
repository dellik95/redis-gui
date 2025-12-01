using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.ExecuteLuaScript;

/// <summary>
/// Handler for executing Lua scripts in Redis.
/// </summary>
public sealed class ExecuteLuaScriptCommandHandler : ICommandHandler<ExecuteLuaScriptCommand, ExecuteLuaScriptResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IConnectionService connectionService;

	public ExecuteLuaScriptCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IConnectionService connectionService)
	{
		this.connectionRepository = connectionRepository;
		this.connectionService = connectionService;
	}

	public async Task<Result<ExecuteLuaScriptResponse>> Handle(
		ExecuteLuaScriptCommand request,
		CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		
		if (connectionResult.IsFailure)
		{
			return Result.Failure<ExecuteLuaScriptResponse>(connectionResult.Error);
		}

		var result = await connectionService.ExecuteLuaScript(
			connectionResult.Value,
			request.Script,
			request.Keys,
			request.Arguments);

		if (result.IsFailure)
		{
			return Result.Failure<ExecuteLuaScriptResponse>(result.Error);
		}

		var scriptResult = result.Value;
		var response = new ExecuteLuaScriptResponse(
			Result: scriptResult.Result,
			Success: true,
			ScriptHash: scriptResult.ScriptHash);

		return Result.Success(response);
	}
}
