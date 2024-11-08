using MediatR;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Abstraction.Messaging;

/// <summary>
/// Represents a command that returns a response of type TResponse
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

/// <summary>
/// Represents a command that doesn't return a response
/// </summary>
public interface ICommand : IRequest<Result>, IBaseCommand
{
}
