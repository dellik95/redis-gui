using MediatR;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Abstraction.Messaging;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
	where TCommand : ICommand<TResponse>
{

}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
	where TCommand : ICommand;