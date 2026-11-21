using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Shared.Mediator
{
	/// <summary>
	/// Represents a request with a response of type TResult. TResult must be either Result or Result{T}.
	/// </summary>
	/// <typeparam name="TResult">The wrapped result type.</typeparam>
	public interface IRequest<TResult> : DispatchWeave.IRequest<TResult> { }

	public interface IResultRequest<TResult> : IRequest<TResult>
	{
		internal TResult Failure(Error error);
	}

	/// <summary>
	/// Represents a command with no response (returns Result).
	/// </summary>
	public interface ICommand : IResultRequest<Result>
	{
		Result IResultRequest<Result>.Failure(Error error) => Result.Failure(error);
	}

	/// <summary>
	/// Represents a command with a response of type TResult (returns Result{TResult}).
	/// </summary>
	/// <typeparam name="TResult">The type of the response.</typeparam>
	public interface ICommand<TResult> : IResultRequest<Result<TResult>>
	{
		Result<TResult> IResultRequest<Result<TResult>>.Failure(Error error) => Result.Failure<TResult>(error);
	}

	/// <summary>
	/// Represents a query with a response of type TResult (returns Result{TResult}).
	/// </summary>
	/// <typeparam name="TResult">The type of the response.</typeparam>
	public interface IQuery<TResult> : IResultRequest<Result<TResult>>
	{
		Result<TResult> IResultRequest<Result<TResult>>.Failure(Error error) => Result.Failure<TResult>(error);
	}

	/// <summary>
	/// Handles a command with no result and returns Result.
	/// </summary>
	/// <typeparam name="TCommand">The command type.</typeparam>
	public interface ICommandHandler<TCommand> : DispatchWeave.IRequestHandler<TCommand, Result> where TCommand : ICommand
	{
	}

	/// <summary>
	/// Handles a command of type TCommand and returns Result{TResult}.
	/// </summary>
	/// <typeparam name="TCommand">The command type.</typeparam>
	/// <typeparam name="TResult">The result type.</typeparam>
	public interface ICommandHandler<TCommand, TResult> : DispatchWeave.IRequestHandler<TCommand, Result<TResult>> where TCommand : ICommand<TResult>
	{
	}

	/// <summary>
	/// Handles a query of type TQuery and returns Result{TResult}.
	/// </summary>
	/// <typeparam name="TQuery">The query type.</typeparam>
	/// <typeparam name="TResult">The result type.</typeparam>
	public interface IQueryHandler<TQuery, TResult> : DispatchWeave.IRequestHandler<TQuery, Result<TResult>> where TQuery : IQuery<TResult>
	{
	}

	/// <summary>
	/// Represents a notification (event) with no response.
	/// </summary>
	public interface INotification : DispatchWeave.INotification { }

	/// <summary>
	/// Handles a notification of type TNotification.
	/// </summary>
	/// <typeparam name="TNotification">The notification type.</typeparam>
	public interface INotificationHandler<TNotification> : DispatchWeave.INotificationHandler<TNotification> where TNotification : DispatchWeave.INotification
	{
	}

	/// <summary>
	/// Defines a pipeline behavior for requests.
	/// </summary>
	/// <typeparam name="TRequest">The request type.</typeparam>
	/// <typeparam name="TResult">The result type.</typeparam>
	public interface IPipelineBehavior<TRequest, TResult> : DispatchWeave.IPipelineBehavior<TRequest, TResult> where TRequest : DispatchWeave.IRequest<TResult>
	{
		Task<TResult> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next);

		Task<TResult> DispatchWeave.IPipelineBehavior<TRequest, TResult>.Handle(
			TRequest request, DispatchWeave.RequestHandlerDelegate<TResult> next, CancellationToken token)
			=> Handle(request, token, next.Invoke);
	}
}
