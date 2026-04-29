using FluentValidation;

namespace RedisGUI.Application.Users.Commands.RegisterUser;

/// <summary>
/// Validator for RegisterUserCommand
/// </summary>
internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
	/// <summary>
	/// Initializes validation rules for RegisterUserCommand
	/// </summary>
	public RegisterUserCommandValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("Email is required")
			.EmailAddress().WithMessage("Email must be a valid email address")
			.MaximumLength(255).WithMessage("Email must not exceed 255 characters");

		RuleFor(x => x.Password)
			.NotEmpty().WithMessage("Password is required")
			.MinimumLength(8).WithMessage("Password must be at least 8 characters long")
			.MaximumLength(128).WithMessage("Password must not exceed 128 characters");

		RuleFor(x => x.FirstName)
			.NotEmpty().WithMessage("First name is required")
			.MaximumLength(100).WithMessage("First name must not exceed 100 characters");

		RuleFor(x => x.LastName)
			.NotEmpty().WithMessage("Last name is required")
			.MaximumLength(100).WithMessage("Last name must not exceed 100 characters");
	}
}
