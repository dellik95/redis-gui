using FluentValidation;
using System.Linq;

namespace RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;

/// <summary>
/// Validator for DeleteMultipleKeysCommand.
/// </summary>
public sealed class DeleteMultipleKeysCommandValidation : AbstractValidator<DeleteMultipleKeysCommand>
{
    /// <summary>
    /// Initializes a new instance of the DeleteMultipleKeysCommandValidation class.
    /// </summary>
    public DeleteMultipleKeysCommandValidation()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty()
            .WithMessage("Connection ID is required.");

        RuleFor(x => x.Keys)
            .NotNull()
            .WithMessage("Keys list is required.")
            .Must(keys => keys != null && keys.Any())
            .WithMessage("At least one key must be provided.")
            .Must(keys => keys == null || keys.Count <= 1000)
            .WithMessage("Cannot delete more than 1000 keys at once.");

        RuleForEach(x => x.Keys)
            .NotEmpty()
            .WithMessage("Key cannot be empty.")
            .MaximumLength(512)
            .WithMessage("Key must not exceed 512 characters.");
    }
}
