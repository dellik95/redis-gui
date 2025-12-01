using FluentValidation;
using RedisGUI.Application.Core.Extensions;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;

namespace RedisGUI.Application.Connections.UpdateConnection;

/// <summary>
/// Validates the parameters for updating an existing Redis connection.
/// </summary>
internal sealed class UpdateConnectionCommandValidation : AbstractValidator<UpdateConnectionCommand>
{
	/// <summary>
	/// Initializes validation rules for connection update.
	/// </summary>
	public UpdateConnectionCommandValidation()
	{
		RuleFor(x => x.Id)
			.NotEmpty()
			.WithMessage("Connection ID is required");

		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(ConnectionName.MaxLength)
			.WithError(DomainErrors.ConnectionName.LongerThanAllowed);

		RuleFor(x => x.Host)
			.NotEmpty()
			.MaximumLength(ConnectionHost.MaxLength)
			.WithError(DomainErrors.ConnectionHost.LongerThanAllowed);

		RuleFor(x => x.Port)
			.GreaterThan(0)
			.LessThanOrEqualTo(65535)
			.WithError(DomainErrors.ConnectionPort.LessThanAllowed);

		RuleFor(x => x.Database)
			.GreaterThanOrEqualTo(0)
			.LessThanOrEqualTo(15)
			.WithMessage("Database number must be between 0 and 15");
	}
}
