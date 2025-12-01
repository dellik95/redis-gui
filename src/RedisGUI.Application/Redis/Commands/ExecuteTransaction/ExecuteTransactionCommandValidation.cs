using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.ExecuteTransaction;

/// <summary>
/// Validator for ExecuteTransactionCommand.
/// </summary>
public sealed class ExecuteTransactionCommandValidation : AbstractValidator<ExecuteTransactionCommand>
{
	public ExecuteTransactionCommandValidation()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required");

		RuleFor(x => x.Operations)
			.NotNull()
			.WithMessage("Operations list is required")
			.NotEmpty()
			.WithMessage("At least one operation is required")
			.Must(ops => ops.Count <= 1000)
			.WithMessage("Transaction cannot contain more than 1000 operations");

		RuleForEach(x => x.Operations)
			.ChildRules(op =>
			{
				op.RuleFor(o => o.Command)
					.NotEmpty()
					.WithMessage("Command is required");

				op.RuleFor(o => o.Key)
					.NotEmpty()
					.WithMessage("Key is required")
					.MaximumLength(1024)
					.WithMessage("Key cannot exceed 1024 characters");
			});
	}
}
