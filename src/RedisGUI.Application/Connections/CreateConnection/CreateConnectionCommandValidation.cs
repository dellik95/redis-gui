using FluentValidation;
using RedisGUI.Application.Core.Extensions;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;

namespace RedisGUI.Application.Connections.CreateConnection;

/// <summary>
/// Validates the parameters for creating a new Redis connection.
/// </summary>
internal sealed class CreateConnectionCommandValidation : AbstractValidator<CreateConnectionCommand>
{
	/// <summary>
	/// Initializes validation rules for connection creation.
	/// </summary>
	public CreateConnectionCommandValidation()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.WithMessage("Connection name is required")
			.MaximumLength(ConnectionName.MaxLength)
			.WithError(DomainErrors.ConnectionName.LongerThanAllowed);

		RuleFor(x => x.Host)
			.NotEmpty()
			.WithMessage("Host is required")
			.MaximumLength(ConnectionHost.MaxLength)
			.WithError(DomainErrors.ConnectionHost.LongerThanAllowed);

		RuleFor(x => x.Port)
			.GreaterThan(0)
			.WithMessage("Port must be greater than 0")
			.LessThanOrEqualTo(65535)
			.WithMessage("Port must be less than or equal to 65535")
			.WithError(DomainErrors.ConnectionPort.LessThanAllowed);

		RuleFor(x => x.Database)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Database number must be greater than or equal to 0")
			.LessThanOrEqualTo(15)
			.WithMessage("Database number must be less than or equal to 15");

		// Validate that if UserName is provided, Password must also be provided
		RuleFor(x => x.UserName)
			.NotEmpty()
			.When(x => !string.IsNullOrWhiteSpace(x.Password))
			.WithMessage("Username is required when password is provided");

		RuleFor(x => x.Password)
			.NotEmpty()
			.When(x => !string.IsNullOrWhiteSpace(x.UserName))
			.WithMessage("Password is required when username is provided");
	}
}
