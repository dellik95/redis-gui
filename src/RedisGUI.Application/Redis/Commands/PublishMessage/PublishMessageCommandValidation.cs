using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.PublishMessage;

/// <summary>
/// Validator for PublishMessageCommand.
/// </summary>
public sealed class PublishMessageCommandValidation : AbstractValidator<PublishMessageCommand>
{
	public PublishMessageCommandValidation()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required");

		RuleFor(x => x.Channel)
			.NotEmpty()
			.WithMessage("Channel name is required")
			.MaximumLength(1024)
			.WithMessage("Channel name cannot exceed 1024 characters");

		RuleFor(x => x.Message)
			.NotNull()
			.WithMessage("Message cannot be null")
			.MaximumLength(512_000_000) // Redis max string size ~512MB
			.WithMessage("Message is too large");
	}
}
