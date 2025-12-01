using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.ExecuteTransaction;

public class ExecuteTransactionCommandValidator : AbstractValidator<ExecuteTransactionCommand>
{
    public ExecuteTransactionCommandValidator()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty()
            .WithMessage("Connection ID is required");

        RuleFor(x => x.Operations)
            .NotEmpty()
            .WithMessage("At least one operation is required")
            .Must(ops => ops.Count <= 100)
            .WithMessage("Cannot exceed 100 operations in a single transaction");
    }
}
