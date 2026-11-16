using RedisGUI.Application.Core.Services;
using FluentValidation;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Messaging;

public sealed record GetChannelQuery(Guid ConnectionId, string Channel) : IQuery<object>;

internal sealed class GetChannelQueryValidator : AbstractValidator<GetChannelQuery>
{
	public GetChannelQueryValidator() => RuleFor(request => request.Channel).NotEmpty().MaximumLength(512).WithMessage("Enter a channel.");
}

internal sealed class GetChannelQueryHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IRedisMessagingOperations messaging)
	: IQueryHandler<GetChannelQuery, object>
{
	public async Task<Result<object>> Handle(GetChannelQuery request, CancellationToken cancellationToken)
	{
		return await (await MessagingConnectionAccess.FindAsync(request.ConnectionId, repository, access, cancellationToken))
			.BindAsync(async connection => Result.Success(await messaging.GetChannelAsync(connection, request.Channel, cancellationToken)));
	}
}
