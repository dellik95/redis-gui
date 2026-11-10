#nullable enable
using RedisGUI.Shared.Primitives;
using RedisGUI.Domain.Primitives;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Mediator
{
	public class Mediator : IMediator
	{
		private readonly HandlerRegistry handlerRegistry;
		private readonly PipelineRegistry pipelineRegistry;
		private readonly NotificationRegistry notificationRegistry;
		private readonly MediatorOptions options;

		public Mediator(HandlerRegistry handlerRegistry, PipelineRegistry pipelineRegistry, NotificationRegistry notificationRegistry, MediatorOptions? options = null)
		{
			this.handlerRegistry = handlerRegistry;
			this.pipelineRegistry = pipelineRegistry;
			this.notificationRegistry = notificationRegistry;
			this.options = options ?? new MediatorOptions();
		}

		public Task<Result> Send(ICommand command, CancellationToken cancellationToken = default)
		{
			return Execute<ICommand, Result>(command, cancellationToken);
		}

		public Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
		{
			return Execute<ICommand<TResult>, Result<TResult>>(command, cancellationToken);
		}

		public Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
		{
			return Execute<IQuery<TResult>, Result<TResult>>(query, cancellationToken);
		}

		private async Task<TWrapped> Execute<TRequest, TWrapped>(TRequest request, CancellationToken cancellationToken)
			where TRequest : IRequest<TWrapped>
		{
			if (!handlerRegistry.TryGetDescriptor(request.GetType(), out var descriptor))
			{
				return FailureResult<TWrapped>("No handler registered for " + request.GetType().Name, "Handler.NotFound");
			}

			var handlerInstance = descriptor.Factory();
			if (handlerInstance == null)
			{
				return FailureResult<TWrapped>("Handler factory produced null for " + request.GetType().Name, "Handler.Null");
			}

			var behaviors = pipelineRegistry.GetBehaviors<TRequest, TWrapped>().Reverse().ToList();
			Func<Task<TWrapped>> handlerDelegate = async () =>
			{
				var result = await descriptor.Invoker(handlerInstance, request!, cancellationToken);
				return (TWrapped)result;
			};

			foreach (var behavior in behaviors)
			{
				var next = handlerDelegate;
				handlerDelegate = () => behavior.Handle(request, cancellationToken, next);
			}

			try
			{
				return await handlerDelegate();
			}
			catch (Exception ex)
			{
				return FailureResult<TWrapped>(ex.Message, "Handler.Exception");
			}
		}

		private static TWrapped FailureResult<TWrapped>(string message, string code)
		{
			var error = new Error(code, message);
			if (typeof(TWrapped) == typeof(Result))
			{
				return (TWrapped)(object)Result.Failure(error);
			}
			if (typeof(TWrapped).IsGenericType && typeof(TWrapped).GetGenericTypeDefinition() == typeof(Result<>))
			{
				var innerType = typeof(TWrapped).GetGenericArguments()[0];
				var failureGeneric = typeof(Result)
					.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
					.First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
					.MakeGenericMethod(innerType);
				return (TWrapped)failureGeneric.Invoke(null, [error])!;
			}
			throw new InvalidOperationException("Wrapped result type must be Result or Result<T>.");
		}

		public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
			where TNotification : INotification
		{
			var handlers = notificationRegistry.GetHandlers<TNotification>().ToList();
			if (options.NotificationExecution == NotificationExecution.Parallel)
			{
				var tasks = handlers.Select(async h =>
				{
					try
					{
						await h.Handle(notification, cancellationToken);
					}
					catch
					{
						// Swallow exceptions to prevent one handler from affecting others
					}
				});
				await Task.WhenAll(tasks);
			}
			else
			{
				foreach (var handler in handlers)
				{
					try
					{
						await handler.Handle(notification, cancellationToken);
					}
					catch
					{
						// Swallow exceptions to prevent one handler from affecting others
					}
				}
			}
		}
	}
}
