using RedisGUI.Shared.Primitives;
using Registration = DispatchWeave.Registration;

namespace RedisGUI.Shared.Mediator;

public sealed class HandlerRegistry : Registration.HandlerRegistry
{
	public void RegisterCommandHandler<T>(ICommandHandler<T> handler) where T : ICommand =>
		Register<T, Result>(() => handler);

	public void RegisterCommandHandler<T>(Func<ICommandHandler<T>> factory) where T : ICommand =>
		Register<T, Result>(factory);

	public void RegisterCommandHandler<T, TValue>(ICommandHandler<T, TValue> handler) where T : ICommand<TValue> =>
		Register<T, Result<TValue>>(() => handler);

	public void RegisterCommandHandler<T, TValue>(Func<ICommandHandler<T, TValue>> factory) where T : ICommand<TValue> =>
		Register<T, Result<TValue>>(factory);

	public void RegisterQueryHandler<T, TValue>(IQueryHandler<T, TValue> handler) where T : IQuery<TValue> =>
		Register<T, Result<TValue>>(() => handler);

	public void RegisterQueryHandler<T, TValue>(Func<IQueryHandler<T, TValue>> factory) where T : IQuery<TValue> =>
		Register<T, Result<TValue>>(factory);

	internal void RegisterCommandHandler(Type request, Func<object> factory) =>
		Register(request, typeof(Result), factory);

	internal void RegisterCommandHandler(Type request, Type value, Func<object> factory) =>
		Register(request, typeof(Result<>).MakeGenericType(value), factory);

	internal void RegisterQueryHandler(Type request, Type value, Func<object> factory) =>
		Register(request, typeof(Result<>).MakeGenericType(value), factory);
}

public sealed class PipelineRegistry : Registration.PipelineRegistry
{
	public new IEnumerable<IPipelineBehavior<T, TResult>> GetBehaviors<T, TResult>(bool includeOpen = true)
		where T : DispatchWeave.IRequest<TResult> => base.GetBehaviors<T, TResult>(includeOpen).Cast<IPipelineBehavior<T, TResult>>();
}

public sealed class NotificationRegistry : Registration.NotificationRegistry
{
	public new IEnumerable<INotificationHandler<T>> GetHandlers<T>() where T : DispatchWeave.INotification =>
		base.GetHandlers<T>().Cast<INotificationHandler<T>>();
}
