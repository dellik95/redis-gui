#nullable enable
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Mediator
{
	public class Mediator : IMediator
	{
		private readonly DispatchWeave.IDispatcher dispatcher;
		private readonly PipelineRegistry? pipelineRegistry;

		public Mediator(DispatchWeave.IDispatcher dispatcher)
		{
			this.dispatcher = dispatcher;
		}

		public Mediator(HandlerRegistry handlerRegistry, PipelineRegistry pipelineRegistry, NotificationRegistry notificationRegistry, MediatorOptions? options = null)
		{
			dispatcher = new DispatchWeave.Dispatcher(new DispatchWeave.Registration.RegistryRoutes(handlerRegistry, pipelineRegistry, notificationRegistry, lastBehaviorOutermost: true), new IsolatedPublisher(options ?? new()));
			this.pipelineRegistry = pipelineRegistry;
		}

		public Task<Result> Send(ICommand command, CancellationToken cancellationToken = default)
		{
			return Execute<ICommand, Result>(command, Result.Failure, cancellationToken);
		}

		public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
		{
			return Execute<ICommand<TResult>, Result<TResult>>(command, Result.Failure<TResult>, cancellationToken);
		}

		public Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
		{
			return Execute<IQuery<TResult>, Result<TResult>>(query, Result.Failure<TResult>, cancellationToken);
		}

		private async Task<TWrapped> Execute<TRequest, TWrapped>(TRequest request, Func<Error, TWrapped> fail, CancellationToken cancellationToken)
			where TRequest : IRequest<TWrapped>
		{
			var behaviors = pipelineRegistry?.GetBehaviors<TRequest, TWrapped>(includeOpen: false) ?? [];
			Func<Task<TWrapped>> handlerDelegate = () => dispatcher.Send(request, cancellationToken);

			foreach (var behavior in behaviors)
			{
				var next = handlerDelegate;
				handlerDelegate = () => behavior.Handle(request, cancellationToken, next);
			}

			try
			{
				return await handlerDelegate();
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (DispatchWeave.HandlerResolutionException ex)
			{
				return fail(new Error(ex.Code, ex.Message));
			}
			catch (Exception ex) when (ex is IResultErrorException)
			{
				return fail(((IResultErrorException)ex).Error);
			}
			catch (Exception ex)
			{
				return fail(new Error("Handler.Exception", ex.Message));
			}
		}

		public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
			where TNotification : INotification => dispatcher.Publish(notification, cancellationToken);

		public sealed class IsolatedPublisher(MediatorOptions options) : DispatchWeave.INotificationPublisher
		{
			public async Task Publish<T>(IEnumerable<DispatchWeave.INotificationHandler<T>> handlers, T notification, CancellationToken token)
				where T : DispatchWeave.INotification
			{
				async Task Invoke(DispatchWeave.INotificationHandler<T> handler)
				{
					try { await handler.Handle(notification, token); }
					catch { /* Redis GUI isolates each subscriber's failure. */ }
				}

				if (options.NotificationExecution == NotificationExecution.Parallel)
					await Task.WhenAll(handlers.Select(Invoke));
				else
					foreach (var handler in handlers)
						await Invoke(handler);
			}
		}
	}
}
