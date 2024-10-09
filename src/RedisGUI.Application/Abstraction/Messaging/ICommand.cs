using MediatR;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Abstraction.Messaging;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;

public interface ICommand : IRequest<Result>, IBaseCommand;