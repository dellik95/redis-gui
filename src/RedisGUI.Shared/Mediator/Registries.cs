#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Shared.Mediator
{
    internal sealed record HandlerDescriptor(
        Func<object> Factory,
        Func<object, object, CancellationToken, Task<object>> Invoker,
        Type WrappedResultType);

    public class HandlerRegistry
    {
        private static readonly MethodInfo registerCommandWithResultMethod = typeof(HandlerRegistry)
            .GetMethod(nameof(RegisterCommandHandlerWithResultGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly MethodInfo registerCommandWithoutResultMethod = typeof(HandlerRegistry)
            .GetMethod(nameof(RegisterCommandHandlerWithoutResultGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly MethodInfo registerQueryHandlerMethod = typeof(HandlerRegistry)
            .GetMethod(nameof(RegisterQueryHandlerGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private readonly ConcurrentDictionary<Type, HandlerDescriptor> handlers = new();

        public void RegisterCommandHandler<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler)
            where TCommand : ICommand<TResult>
        {
            RegisterHandler<TCommand, Result<TResult>, ICommandHandler<TCommand, TResult>>(
                () => handler,
                static (typedHandler, command, token) => typedHandler.Handle(command, token));
        }

        public void RegisterCommandHandler<TCommand, TResult>(Func<ICommandHandler<TCommand, TResult>> factory)
            where TCommand : ICommand<TResult>
        {
            RegisterHandler<TCommand, Result<TResult>, ICommandHandler<TCommand, TResult>>(
                () => factory(),
                static (typedHandler, command, token) => typedHandler.Handle(command, token));
        }

        public void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler)
            where TCommand : ICommand
        {
            RegisterHandler<TCommand, Result, ICommandHandler<TCommand>>(
                () => handler,
                static (typedHandler, command, token) => typedHandler.Handle(command, token));
        }

        public void RegisterCommandHandler<TCommand>(Func<ICommandHandler<TCommand>> factory)
            where TCommand : ICommand
        {
            RegisterHandler<TCommand, Result, ICommandHandler<TCommand>>(
                () => factory(),
                static (typedHandler, command, token) => typedHandler.Handle(command, token));
        }

        internal void RegisterCommandHandler(Type commandType, Func<object> factory)
        {
            registerCommandWithoutResultMethod
                .MakeGenericMethod(commandType)
                .Invoke(this, [factory]);
        }

        internal void RegisterCommandHandler(Type commandType, Type resultType, Func<object> factory)
        {
            registerCommandWithResultMethod
                .MakeGenericMethod(commandType, resultType)
                .Invoke(this, [factory]);
        }

        public void RegisterQueryHandler<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler)
            where TQuery : IQuery<TResult>
        {
            RegisterHandler<TQuery, Result<TResult>, IQueryHandler<TQuery, TResult>>(
                () => handler,
                static (typedHandler, query, token) => typedHandler.Handle(query, token));
        }

        public void RegisterQueryHandler<TQuery, TResult>(Func<IQueryHandler<TQuery, TResult>> factory)
            where TQuery : IQuery<TResult>
        {
            RegisterHandler<TQuery, Result<TResult>, IQueryHandler<TQuery, TResult>>(
                () => factory(),
                static (typedHandler, query, token) => typedHandler.Handle(query, token));
        }

        internal void RegisterQueryHandler(Type queryType, Type resultType, Func<object> factory)
        {
            registerQueryHandlerMethod
                .MakeGenericMethod(queryType, resultType)
                .Invoke(this, [factory]);
        }

        /// <summary>
        /// Tries to get a handler descriptor by the concrete request type.
        /// </summary>
        /// <param name="requestType">The concrete request type.</param>
        /// <param name="descriptor">The descriptor containing the factory and invoker.</param>
        /// <returns>True if a descriptor exists for the request type.</returns>
        internal bool TryGetDescriptor(Type requestType, [NotNullWhen(true)] out HandlerDescriptor? descriptor)
        {
            return handlers.TryGetValue(requestType, out descriptor);
        }

        private void RegisterHandler<TRequest, TResultWrapped, THandler>(
            Func<object> factory,
            Func<THandler, TRequest, CancellationToken, Task<TResultWrapped>> invoker)
            where TRequest : IRequest<TResultWrapped>
        {
            handlers[typeof(TRequest)] = new HandlerDescriptor(
                factory,
                async (handlerObj, requestObj, token) =>
                {
                    if (handlerObj is not THandler typedHandler)
                    {
                        throw new InvalidOperationException($"Handler instance does not implement expected type {typeof(THandler).FullName}.");
                    }

                    if (requestObj is not TRequest typedRequest)
                    {
                        throw new InvalidOperationException($"Request instance does not match expected type {typeof(TRequest).FullName}.");
                    }

                    var result = await invoker(typedHandler, typedRequest, token);
                    return (object?)result ?? throw new InvalidOperationException($"Handler {typeof(THandler).FullName} returned null result for {typeof(TRequest).FullName}.");
                },
                typeof(TResultWrapped));
        }

        private void RegisterCommandHandlerWithResultGeneric<TCommand, TResult>(Func<object> factory)
            where TCommand : ICommand<TResult>
        {
            RegisterHandler<TCommand, Result<TResult>, ICommandHandler<TCommand, TResult>>(
                factory,
                static (typedHandler, command, token) => typedHandler.Handle(command, token));
        }

        private void RegisterCommandHandlerWithoutResultGeneric<TCommand>(Func<object> factory)
            where TCommand : ICommand
        {
            RegisterHandler<TCommand, Result, ICommandHandler<TCommand>>(
                factory,
                static (typedHandler, command, token) => typedHandler.Handle(command, token));
        }

        private void RegisterQueryHandlerGeneric<TQuery, TResult>(Func<object> factory)
            where TQuery : IQuery<TResult>
        {
            RegisterHandler<TQuery, Result<TResult>, IQueryHandler<TQuery, TResult>>(
                factory,
                static (typedHandler, query, token) => typedHandler.Handle(query, token));
        }
    }

    public class PipelineRegistry
    {
        private sealed record PipelineRegistration(Type InterfaceType, Func<object> Factory);
        private sealed record OpenPipelineRegistration(Type BehaviorType, Func<Type, object?> Factory);

        private readonly ConcurrentBag<PipelineRegistration> behaviors = new();
        private readonly ConcurrentBag<OpenPipelineRegistration> openBehaviors = new();

        public void AddBehavior<TRequest, TResult>(IPipelineBehavior<TRequest, TResult> behavior)
            where TRequest : IRequest<TResult>
        {
            var interfaceType = typeof(IPipelineBehavior<TRequest, TResult>);
            behaviors.Add(new PipelineRegistration(interfaceType, () => behavior));
        }

        public void AddBehavior<TRequest, TResult>(Func<IPipelineBehavior<TRequest, TResult>> factory)
            where TRequest : IRequest<TResult>
        {
            var interfaceType = typeof(IPipelineBehavior<TRequest, TResult>);
            behaviors.Add(new PipelineRegistration(interfaceType, () => factory()));
        }

        internal void AddBehavior(Type requestType, Type resultType, Func<object> factory)
        {
            var interfaceType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, resultType);
            behaviors.Add(new PipelineRegistration(interfaceType, factory));
        }

        internal void AddOpenBehavior(Type behaviorType, Func<Type, object?> factory)
        {
            if (!behaviorType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Behavior type must be an open generic definition.", nameof(behaviorType));
            }

            openBehaviors.Add(new OpenPipelineRegistration(behaviorType, factory));
        }

        public IEnumerable<IPipelineBehavior<TRequest, TResult>> GetBehaviors<TRequest, TResult>()
            where TRequest : IRequest<TResult>
        {
            var interfaceType = typeof(IPipelineBehavior<TRequest, TResult>);
            var resolved = behaviors
                .Where(b => b.InterfaceType == interfaceType)
                .Select(b => (IPipelineBehavior<TRequest, TResult>)b.Factory());

            var openResolved = openBehaviors
				.Select(InstantiateOpenBehavior<TRequest, TResult>)
                .Where(static behavior => behavior is not null)
                .Cast<IPipelineBehavior<TRequest, TResult>>();

            return resolved.Concat(openResolved);
        }

        private static IPipelineBehavior<TRequest, TResult>? InstantiateOpenBehavior<TRequest, TResult>(OpenPipelineRegistration registration)
            where TRequest : IRequest<TResult>
        {
            Type closedType;
            try
            {
                closedType = registration.BehaviorType.MakeGenericType(typeof(TRequest), typeof(TResult));
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (TypeLoadException)
            {
                return null;
            }

            var instance = registration.Factory(closedType);
            if (instance is null)
            {
                throw new InvalidOperationException($"Factory returned null for open pipeline behavior {registration.BehaviorType.FullName}.");
            }

            if (instance is not IPipelineBehavior<TRequest, TResult> typedBehavior)
            {
                throw new InvalidOperationException($"Resolved instance {instance.GetType().FullName} does not implement IPipelineBehavior<{typeof(TRequest).FullName}, {typeof(TResult).FullName}>.");
            }

            return typedBehavior;
        }
    }

    public class NotificationRegistry
    {
        private readonly ConcurrentDictionary<Type, ConcurrentBag<Func<object>>> handlers = new();

        public void AddHandler<TNotification>(INotificationHandler<TNotification> handler)
            where TNotification : INotification
        {
            var bag = handlers.GetOrAdd(typeof(TNotification), _ => new ConcurrentBag<Func<object>>());
            bag.Add(() => handler);
        }

        public void AddHandler<TNotification>(Func<INotificationHandler<TNotification>> factory)
            where TNotification : INotification
        {
            var bag = handlers.GetOrAdd(typeof(TNotification), _ => new ConcurrentBag<Func<object>>());
            bag.Add(() => factory());
        }

        internal void AddHandler(Type notificationType, Func<object> factory)
        {
            var bag = handlers.GetOrAdd(notificationType, _ => new ConcurrentBag<Func<object>>());
            bag.Add(factory);
        }

        public IEnumerable<INotificationHandler<TNotification>> GetHandlers<TNotification>()
            where TNotification : INotification
        {
            if (handlers.TryGetValue(typeof(TNotification), out var handlerBag))
            {
                return handlerBag.Select(factory => (INotificationHandler<TNotification>)factory());
            }

            return Enumerable.Empty<INotificationHandler<TNotification>>();
        }
    }
}
