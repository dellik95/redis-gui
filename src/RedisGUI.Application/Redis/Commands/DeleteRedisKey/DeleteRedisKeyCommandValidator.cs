using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.DeleteRedisKey;

/// <summary>
/// Validates the parameters for deleting a Redis key.
/// </summary>
internal sealed class DeleteRedisKeyCommandValidator : AbstractValidator<DeleteRedisKeyCommand>
{
	/// <summary>
	/// Initializes validation rules for deleting Redis keys.
	/// </summary>
	public DeleteRedisKeyCommandValidator()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required");

		RuleFor(x => x.Key)
			.NotEmpty()
			.WithMessage("Key is required")
			.MaximumLength(512)
			.WithMessage("Key cannot exceed 512 characters");
	}
}
