using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

/// <summary>
/// Validator for DeleteKeysByPatternCommand.
/// </summary>
public sealed class DeleteKeysByPatternCommandValidation : AbstractValidator<DeleteKeysByPatternCommand>
{
    /// <summary>
    /// Initializes a new instance of the DeleteKeysByPatternCommandValidation class.
    /// </summary>
    public DeleteKeysByPatternCommandValidation()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty()
            .WithMessage("Connection ID is required.");

        RuleFor(x => x.Pattern)
            .NotEmpty()
            .WithMessage("Pattern is required.")
            .MaximumLength(1024)
            .WithMessage("Pattern must not exceed 1024 characters.")
            .Must(pattern => !string.IsNullOrWhiteSpace(pattern))
            .WithMessage("Pattern cannot be empty or whitespace.");
    }
}
