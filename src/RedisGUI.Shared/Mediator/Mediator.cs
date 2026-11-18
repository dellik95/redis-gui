#nullable enable
using RedisGUI.Shared.Primitives;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Mediator
{
	public class Mediator : IMediator
	{
		private static readonly MethodInfo concreteExecutionMethod = typeof(Mediator).GetMethod(nameof(ExecuteConcrete), BindingFlags.Instance | BindingFlags.NonPublic)!;
		private static readonly MethodInfo genericFailureMethod = typeof(Result)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
		private static readonly ConcurrentDictionary<(Type Request, Type Result), MethodInfo> concreteMethods = new();
		private static readonly ConcurrentDictionary<Type, Func<Error, object>> failureFactories = new();
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

			var behaviors = pipelineRegistry.GetBehaviors<TRequest, TWrapped>(includeOpen: false);
			var method = concreteMethods.GetOrAdd((request.GetType(), typeof(TWrapped)),
				static key => concreteExecutionMethod.MakeGenericMethod(key.Request, key.Result));
			Func<Task<TWrapped>> handlerDelegate = () => (Task<TWrapped>)method.Invoke(this, [request, handlerInstance, descriptor, cancellationToken])!;

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
			catch (Exception ex) when (ex is IResultErrorException)
			{
				return FailureResult<TWrapped>(((IResultErrorException)ex).Error);
			}
			catch (Exception ex)
			{
				return FailureResult<TWrapped>(ex.Message, "Handler.Exception");
			}
		}

		private Task<TWrapped> ExecuteConcrete<TRequest, TWrapped>(TRequest request, object handlerInstance,
			HandlerDescriptor descriptor, CancellationToken cancellationToken) where TRequest : IRequest<TWrapped>
		{
			Func<Task<TWrapped>> next = async () => (TWrapped)await descriptor.Invoker(handlerInstance, request!, cancellationToken);
			foreach (var behavior in pipelineRegistry.GetBehaviors<TRequest, TWrapped>())
			{
				var inner = next;
				next = () => behavior.Handle(request, cancellationToken, inner);
			}
			return next();
		}

		private static TWrapped FailureResult<TWrapped>(string message, string code)
		{
			return FailureResult<TWrapped>(new Error(code, message));
		}

		private static TWrapped FailureResult<TWrapped>(Error error)
		{
			if (typeof(TWrapped) == typeof(Result))
			{
				return (TWrapped)(object)Result.Failure(error);
			}
			if (typeof(TWrapped).IsGenericType && typeof(TWrapped).GetGenericTypeDefinition() == typeof(Result<>))
			{
				var factory = failureFactories.GetOrAdd(typeof(TWrapped), static wrappedType =>
				{
					var innerType = wrappedType.GetGenericArguments()[0];
					var closed = genericFailureMethod.MakeGenericMethod(innerType);
					return closed.CreateDelegate<Func<Error, object>>();
				});
				return (TWrapped)factory(error);
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
