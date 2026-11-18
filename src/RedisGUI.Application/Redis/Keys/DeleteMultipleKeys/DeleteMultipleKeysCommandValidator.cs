using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;

/// <summary>
/// Validator for DeleteMultipleKeysCommand.
/// </summary>
public sealed class DeleteMultipleKeysCommandValidator : AbstractValidator<DeleteMultipleKeysCommand>
{
	/// <summary>
	/// Initializes a new instance of the DeleteMultipleKeysCommandValidator class.
	/// </summary>
	public DeleteMultipleKeysCommandValidator()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required.");

		RuleFor(x => x.Keys)
			.NotNull()
			.WithMessage("Keys list is required.")
			.Must(keys => keys != null && keys.Count > 0)
			.WithMessage("At least one key must be provided.")
			.Must(keys => keys == null || keys.Count <= 1000)
			.WithMessage("Cannot delete more than 1000 keys at once.");

		When(x => x.KeysBase64 == null || x.KeysBase64.Count == 0, () =>
		{
			RuleForEach(x => x.Keys)
				.NotEmpty()
				.WithMessage("Key cannot be empty.")
				.MaximumLength(512)
				.WithMessage("Key must not exceed 512 characters.");
		});

		When(x => x.KeysBase64 != null && x.KeysBase64.Count > 0, () =>
		{
			RuleFor(x => x.KeysBase64!)
				.Must((cmd, b64) => cmd.Keys != null && b64.Count == cmd.Keys.Count)
				.WithMessage("KeysBase64 count must match Keys count.");
		});
	}
}
