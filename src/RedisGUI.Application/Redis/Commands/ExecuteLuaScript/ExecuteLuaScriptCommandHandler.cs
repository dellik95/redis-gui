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
	private readonly IRedisScriptService scriptService;

	public ExecuteLuaScriptCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisScriptService scriptService)
	{
		this.connectionRepository = connectionRepository;
		this.scriptService = scriptService;
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

		var result = await scriptService.ExecuteLuaScriptAsync(
			connectionResult.Value,
			request.Script,
			request.Keys,
			request.Arguments,
			cancellationToken);

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
