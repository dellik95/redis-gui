

using FluentValidation;
using RedisGUI.Application.Core.Extensions;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;



namespace RedisGUI.Application.Connections.CheckConnection;

internal sealed class CheckConnectionCommandValidator : AbstractValidator<CheckConnectionCommand>
{
	public CheckConnectionCommandValidator()
	{
		RuleFor(x => x.Host).MaximumLength(ConnectionHost.MaxLength)
			.WithError(DomainErrors.ConnectionHost.LongerThanAllowed);

		RuleFor(x => x.Port).GreaterThan(0).WithError(DomainErrors.ConnectionPort.LessThanAllowed);
	}
}
