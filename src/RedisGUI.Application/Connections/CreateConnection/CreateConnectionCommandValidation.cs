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
		RuleFor(x => x.Name).MaximumLength(ConnectionName.MaxLength)
			.WithError(DomainErrors.ConnectionName.LongerThanAllowed);

		RuleFor(x => x.Host).MaximumLength(ConnectionHost.MaxLength)
			.WithError(DomainErrors.ConnectionHost.LongerThanAllowed);

		RuleFor(x => x.Port).GreaterThan(0).WithError(DomainErrors.ConnectionPort.LessThanAllowed);
	}
}
