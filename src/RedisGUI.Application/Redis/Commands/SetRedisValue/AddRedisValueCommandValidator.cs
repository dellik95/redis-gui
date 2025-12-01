using FluentValidation;
using System;

namespace RedisGUI.Application.Redis.Commands.SetRedisValue;

/// <summary>
/// Validates the parameters for setting a Redis value.
/// </summary>
internal sealed class AddRedisValueCommandValidator : AbstractValidator<AddRedisValueCommand>
{
	/// <summary>
	/// Initializes validation rules for setting Redis values.
	/// </summary>
	public AddRedisValueCommandValidator()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required");

		RuleFor(x => x.Key)
			.NotEmpty()
			.WithMessage("Key is required")
			.MaximumLength(512)
			.WithMessage("Key cannot exceed 512 characters");

		RuleFor(x => x.Value)
			.NotNull()
			.WithMessage("Value cannot be null");

		RuleFor(x => x.Ttl)
			.GreaterThan(TimeSpan.Zero)
			.When(x => x.Ttl.HasValue)
			.WithMessage("TTL must be greater than zero when specified");
	}
}
