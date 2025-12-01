using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

public class DeleteKeysByPatternCommandValidator : AbstractValidator<DeleteKeysByPatternCommand>
{
    public DeleteKeysByPatternCommandValidator()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty()
            .WithMessage("Connection ID is required");

        RuleFor(x => x.Pattern)
            .NotEmpty()
            .WithMessage("Pattern is required")
            .MaximumLength(256)
            .WithMessage("Pattern cannot exceed 256 characters");
    }
}
