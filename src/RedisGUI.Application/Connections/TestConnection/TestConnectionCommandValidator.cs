using FluentValidation;
using RedisGUI.Application.Core.Extensions;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;

namespace RedisGUI.Application.Connections.TestConnection;

internal sealed class TestConnectionCommandValidator : AbstractValidator<TestConnectionCommand>
{
	public TestConnectionCommandValidator()
	{
		RuleFor(x => x.Name).MaximumLength(ConnectionName.MaxLength)
			.WithError(DomainErrors.ConnectionName.LongerThanAllowed);

		RuleFor(x => x.Host).MaximumLength(ConnectionHost.MaxLength)
			.WithError(DomainErrors.ConnectionHost.LongerThanAllowed);

		RuleFor(x => x.Port).GreaterThan(0).WithError(DomainErrors.ConnectionPort.LessThanAllowed);
	}
}