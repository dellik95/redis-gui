using RedisGUI.Application.Core.Services;
using FluentValidation;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Messaging;

public sealed record GetStreamQuery(Guid ConnectionId, string Key) : IQuery<object>;

internal sealed class GetStreamQueryValidator : AbstractValidator<GetStreamQuery>
{
	public GetStreamQueryValidator() => RuleFor(request => request.Key).NotEmpty().MaximumLength(512).WithMessage("Enter a stream key.");
}

internal sealed class GetStreamQueryHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IRedisMessagingOperations messaging)
	: IQueryHandler<GetStreamQuery, object>
{
	public async Task<Result<object>> Handle(GetStreamQuery request, CancellationToken cancellationToken)
	{
		return await (await MessagingConnectionAccess.FindAsync(request.ConnectionId, repository, access, cancellationToken))
			.BindAsync(async connection => Result.Success(await messaging.GetStreamAsync(connection, request.Key, cancellationToken)));
	}
}
