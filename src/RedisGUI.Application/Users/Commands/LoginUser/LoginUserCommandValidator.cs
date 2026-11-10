using FluentValidation;

namespace RedisGUI.Application.Users.Commands.LoginUser;

/// <summary>
/// Validator for LoginUserCommand
/// </summary>
internal sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
	/// <summary>
	/// Initializes validation rules for LoginUserCommand
	/// </summary>
	public LoginUserCommandValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("Email is required")
			.EmailAddress().WithMessage("Email must be a valid email address");

		RuleFor(x => x.Password)
			.NotEmpty().WithMessage("Password is required");
	}
}
