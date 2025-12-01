using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedisGUI.Shared.Mediator
{
	public class MediatorBuilder
	{
		private readonly List<Action<Func<Type, object?>, HandlerRegistry, PipelineRegistry, NotificationRegistry>> registrations = [];
		private readonly MediatorOptions options = new();

		public MediatorBuilder RegisterCommandHandler<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler)
			where TCommand : ICommand<TResult>
		{
			registrations.Add((_, handlers, _, _) => handlers.RegisterCommandHandler(handler));
			return this;
		}

		public MediatorBuilder RegisterCommandHandler<TCommand, TResult>(Func<ICommandHandler<TCommand, TResult>> factory)
			where TCommand : ICommand<TResult>
		{
			registrations.Add((_, handlers, _, _) => handlers.RegisterCommandHandler(factory));
			return this;
		}

		public MediatorBuilder RegisterCommandHandler<TCommand, TResult, TImplementation>()
			where TCommand : ICommand<TResult>
			where TImplementation : class, ICommandHandler<TCommand, TResult>
		{
			registrations.Add((resolver, handlers, _, _) =>
				handlers.RegisterCommandHandler(typeof(TCommand), typeof(TResult), () => Resolve(resolver, typeof(TImplementation))));
			return this;
		}

		public MediatorBuilder RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler)
			where TCommand : ICommand
		{
			registrations.Add((_, handlers, _, _) => handlers.RegisterCommandHandler(handler));
			return this;
		}

		public MediatorBuilder RegisterCommandHandler<TCommand>(Func<ICommandHandler<TCommand>> factory)
			where TCommand : ICommand
		{
			registrations.Add((_, handlers, _, _) => handlers.RegisterCommandHandler(factory));
			return this;
		}

		public MediatorBuilder RegisterCommandHandler<TCommand, TImplementation>()
			where TCommand : ICommand
			where TImplementation : class, ICommandHandler<TCommand>
		{
			registrations.Add((resolver, handlers, _, _) =>
				handlers.RegisterCommandHandler(typeof(TCommand), () => Resolve(resolver, typeof(TImplementation))));
			return this;
		}

		public MediatorBuilder RegisterQueryHandler<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler)
			where TQuery : IQuery<TResult>
		{
			registrations.Add((_, handlers, _, _) => handlers.RegisterQueryHandler(handler));
			return this;
		}

		public MediatorBuilder RegisterQueryHandler<TQuery, TResult>(Func<IQueryHandler<TQuery, TResult>> factory)
			where TQuery : IQuery<TResult>
		{
			registrations.Add((_, handlers, _, _) => handlers.RegisterQueryHandler(factory));
			return this;
		}

		public MediatorBuilder RegisterQueryHandler<TQuery, TResult, TImplementation>()
			where TQuery : IQuery<TResult>
			where TImplementation : class, IQueryHandler<TQuery, TResult>
		{
			registrations.Add((resolver, handlers, _, _) =>
				handlers.RegisterQueryHandler(typeof(TQuery), typeof(TResult), () => Resolve(resolver, typeof(TImplementation))));
			return this;
		}

		public MediatorBuilder AddNotificationHandler<TNotification>(INotificationHandler<TNotification> handler)
			where TNotification : INotification
		{
			registrations.Add((_, _, _, notifications) => notifications.AddHandler(handler));
			return this;
		}

		public MediatorBuilder AddNotificationHandler<TNotification>(Func<INotificationHandler<TNotification>> factory)
			where TNotification : INotification
		{
			registrations.Add((_, _, _, notifications) => notifications.AddHandler(factory));
			return this;
		}

		public MediatorBuilder AddNotificationHandler<TNotification, THandler>()
			where TNotification : INotification
			where THandler : class, INotificationHandler<TNotification>
		{
			registrations.Add((resolver, _, _, notifications) =>
				notifications.AddHandler(typeof(TNotification), () => Resolve(resolver, typeof(THandler))));
			return this;
		}

		public MediatorBuilder AddPipelineBehavior<TRequest, TResult>(IPipelineBehavior<TRequest, TResult> behavior)
			where TRequest : IRequest<TResult>
		{
			registrations.Add((_, _, pipelines, _) => pipelines.AddBehavior(behavior));
			return this;
		}

		public MediatorBuilder AddPipelineBehavior<TRequest, TResult>(Func<IPipelineBehavior<TRequest, TResult>> factory)
			where TRequest : IRequest<TResult>
		{
			registrations.Add((_, _, pipelines, _) => pipelines.AddBehavior(factory));
			return this;
		}

		public MediatorBuilder AddPipelineBehavior<TRequest, TResult, TBehavior>()
			where TRequest : IRequest<TResult>
			where TBehavior : class, IPipelineBehavior<TRequest, TResult>
		{
			registrations.Add((resolver, _, pipelines, _) =>
				pipelines.AddBehavior(typeof(TRequest), typeof(TResult), () => Resolve(resolver, typeof(TBehavior))));
			return this;
		}

		public MediatorBuilder Configure(Action<MediatorOptions> configure)
		{
			configure(options);
			return this;
		}
		public MediatorBuilder RegisterHandlersFromAssembly(Assembly assembly)
		{
			var targetDefs = new[]
			{
				typeof(ICommandHandler<>),
				typeof(ICommandHandler<,>),
				typeof(IQueryHandler<,>),
				typeof(INotificationHandler<>),
				typeof(IPipelineBehavior<,>)
			};

			static bool IsConcrete(Type t) => t.IsClass && !t.IsAbstract;
			static bool IsOpenGenericDefinition(Type t) => t.IsGenericTypeDefinition;
			static bool IsTargetInterface(Type iface, Type[] targets) =>
				iface.IsGenericType && targets.Contains(iface.GetGenericTypeDefinition());

			var relevantPairs = assembly.GetTypes()
				.Where(IsConcrete)
				.Where(t => !IsOpenGenericDefinition(t) || t.GetInterfaces()
					.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
				.SelectMany(type => type.GetInterfaces()
					.Where(iface => IsTargetInterface(iface, targetDefs))
					.Select(iface => new { Type = type, Interface = iface }))
				.ToArray();

			registrations.Add((resolver, handlers, pipelines, notifications) =>
			{
				foreach (var pair in relevantPairs)
				{
					var type = pair.Type;
					var iface = pair.Interface;
					var definition = iface.GetGenericTypeDefinition();

					if (definition == typeof(IPipelineBehavior<,>))
					{
						if (type.IsGenericTypeDefinition)
						{
							pipelines.AddOpenBehavior(type, closedType => Resolve(resolver, closedType));
						}
						else
						{
							var args = iface.GetGenericArguments();
							pipelines.AddBehavior(args[0], args[1], () => Resolve(resolver, type));
						}
						continue;
					}

					switch (definition)
					{
						case var d when d == typeof(ICommandHandler<,>):
							{
								var args = iface.GetGenericArguments();
								handlers.RegisterCommandHandler(args[0], args[1], () => Resolve(resolver, type));
								break;
							}
						case var d when d == typeof(ICommandHandler<>):
							{
								var args = iface.GetGenericArguments();
								handlers.RegisterCommandHandler(args[0], () => Resolve(resolver, type));
								break;
							}
						case var d when d == typeof(IQueryHandler<,>):
							{
								var args = iface.GetGenericArguments();
								handlers.RegisterQueryHandler(args[0], args[1], () => Resolve(resolver, type));
								break;
							}
						case var d when d == typeof(INotificationHandler<>):
							{
								var args = iface.GetGenericArguments();
								notifications.AddHandler(args[0], () => Resolve(resolver, type));
								break;
							}
						default:
							break;
					}
				}
			});

			return this;
		}

		public MediatorBuilder RegisterHandlersFromAssemblies(params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
			{
				RegisterHandlersFromAssembly(assembly);
			}

			return this;
		}

		public IMediator Build(Func<Type, object?>? typeResolver = null)
		{
			var handlerRegistry = new HandlerRegistry();
			var pipelineRegistry = new PipelineRegistry();
			var notificationRegistry = new NotificationRegistry();

			var resolver = typeResolver ?? DefaultResolver;

			foreach (var registration in registrations)
			{
				registration(resolver, handlerRegistry, pipelineRegistry, notificationRegistry);
			}

			var optionsCopy = new MediatorOptions
			{
				NotificationExecution = options.NotificationExecution
			};

			return new Mediator(handlerRegistry, pipelineRegistry, notificationRegistry, optionsCopy);
		}

		private static object DefaultResolver(Type type)
		{
			var instance = Activator.CreateInstance(type);
			if (instance == null)
			{
				throw new InvalidOperationException($"Unable to create an instance of type {type.FullName}.");
			}

			return instance;
		}

		private static object Resolve(Func<Type, object?> resolver, Type implementationType)
		{
			var instance = resolver(implementationType);
			if (instance == null)
			{
				throw new InvalidOperationException($"Resolver returned null for type {implementationType.FullName}.");
			}

			return instance;
		}
	}
}
