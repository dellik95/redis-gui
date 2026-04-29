using FluentValidation;
using RedisGUI.Application.Core.Extensions;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;

namespace RedisGUI.Application.Connections.CheckConnection;

/// <summary>
/// Validates the parameters for checking a Redis connection.
/// </summary>
internal sealed class CheckConnectionCommandValidator : AbstractValidator<CheckConnectionCommand>
{
	/// <summary>
	/// Initializes validation rules for connection checking.
	/// </summary>
	public CheckConnectionCommandValidator()
	{
		RuleFor(x => x.Host)
			.NotEmpty()
			.WithMessage("Host is required")
			.MaximumLength(ConnectionHost.MaxLength)
			.WithError(DomainErrors.ConnectionHost.LongerThanAllowed);

		RuleFor(x => x.Port)
			.InclusiveBetween(RedisConstants.MinPort, RedisConstants.MaxPort)
			.WithMessage($"Port must be between {RedisConstants.MinPort} and {RedisConstants.MaxPort}")
			.WithError(DomainErrors.ConnectionPort.LessThanAllowed);

		RuleFor(x => x.Database)
			.InclusiveBetween(0, RedisConstants.MaxDatabases - 1)
			.WithMessage($"Database number must be between 0 and {RedisConstants.MaxDatabases - 1}");

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
