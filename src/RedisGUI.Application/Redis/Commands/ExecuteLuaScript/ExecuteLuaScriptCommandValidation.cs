using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.ExecuteLuaScript;

/// <summary>
/// Validator for ExecuteLuaScriptCommand.
/// </summary>
public sealed class ExecuteLuaScriptCommandValidation : AbstractValidator<ExecuteLuaScriptCommand>
{
	public ExecuteLuaScriptCommandValidation()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required");

		RuleFor(x => x.Script)
			.NotEmpty()
			.WithMessage("Lua script is required")
			.MaximumLength(512_000)
			.WithMessage("Script size cannot exceed 512KB");

		RuleFor(x => x.Keys)
			.NotNull()
			.WithMessage("Keys array is required (can be empty)");

		RuleFor(x => x.Arguments)
			.NotNull()
			.WithMessage("Arguments array is required (can be empty)");
	}
}
