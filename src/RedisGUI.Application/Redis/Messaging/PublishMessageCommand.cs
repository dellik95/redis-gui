using FluentResults;
using RedisGUI.Application.Core.Services;
using FluentValidation;
using RedisGUI.Domain.Connection;
using DispatchWeave;
using DispatchWeave.Results;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Messaging;

public sealed record PublishMessageCommand(Guid ConnectionId, string Channel, string Message) : ICommand<long>;

internal sealed class PublishMessageCommandValidator : AbstractValidator<PublishMessageCommand>
{
	public PublishMessageCommandValidator()
	{
		RuleFor(request => request.Channel).NotEmpty().MaximumLength(512).WithMessage("Invalid channel or message.");
		RuleFor(request => request.Message).NotNull().MaximumLength(1048576).WithMessage("Invalid channel or message.");
	}
}

internal sealed class PublishMessageCommandHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IRedisMessagingOperations messaging)
	: ICommandHandler<PublishMessageCommand, long>
{
	public async Task<Result<long>> Handle(PublishMessageCommand request, CancellationToken cancellationToken)
	{
		return await (await MessagingConnectionAccess.FindAsync(request.ConnectionId, repository, access, cancellationToken))
			.BindAsync(async connection => connection.IsReadOnly
				? Result.Fail<long>(new DomainError("Connection.Forbidden", "Access to the connection is denied."))
				: Result.Ok(await messaging.PublishAsync(connection, request.Channel, request.Message, cancellationToken)));
	}
}
