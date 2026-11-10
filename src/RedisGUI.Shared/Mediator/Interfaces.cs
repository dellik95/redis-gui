using System;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Shared.Mediator
{
    /// <summary>
    /// Represents a request with a response of type TResult. TResult must be either Result or Result{T}.
    /// </summary>
    /// <typeparam name="TResult">The wrapped result type.</typeparam>
    public interface IRequest<TResult> { }

    /// <summary>
    /// Represents a command with no response (returns Result).
    /// </summary>
    public interface ICommand : IRequest<Result> { }

    /// <summary>
    /// Represents a command with a response of type TResult (returns Result{TResult}).
    /// </summary>
    /// <typeparam name="TResult">The type of the response.</typeparam>
    public interface ICommand<TResult> : IRequest<Result<TResult>> { }

    /// <summary>
    /// Represents a query with a response of type TResult (returns Result{TResult}).
    /// </summary>
    /// <typeparam name="TResult">The type of the response.</typeparam>
    public interface IQuery<TResult> : IRequest<Result<TResult>> { }

    /// <summary>
    /// Handles a command with no result and returns Result.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handles a command of type TCommand and returns Result{TResult}.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Handles a query of type TQuery and returns Result{TResult}.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents a notification (event) with no response.
    /// </summary>
    public interface INotification { }

    /// <summary>
    /// Handles a notification of type TNotification.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    public interface INotificationHandler<TNotification> where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Defines a pipeline behavior for requests.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface IPipelineBehavior<TRequest, TResult> where TRequest : IRequest<TResult>
    {
        Task<TResult> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next);
    }
}
