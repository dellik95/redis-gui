using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Tests.Mediator
{
	[TestFixture]
	public class MediatorBuilderTests
	{
		private record TestCommand(Guid Id) : ICommand<Guid>;
		private record TestCommandWithoutResult(string Name) : ICommand;
		private record TestQuery(string Id) : IQuery<string>;
		private record TestNotification(string Message) : INotification;

		private class TestCommandHandler : ICommandHandler<TestCommand, Guid>
		{
			public Task<Result<Guid>> Handle(TestCommand command, CancellationToken cancellationToken)
			{
				return Task.FromResult(Result.Success(command.Id));
			}
		}

		private class TestCommandHandlerWithoutResult : ICommandHandler<TestCommandWithoutResult>
		{
			public bool WasCalled { get; private set; }

			public Task<Result> Handle(TestCommandWithoutResult command, CancellationToken cancellationToken)
			{
				WasCalled = true;
				return Task.FromResult(Result.Success());
			}
		}

		private class TestQueryHandler : IQueryHandler<TestQuery, string>
		{
			public Task<Result<string>> Handle(TestQuery query, CancellationToken cancellationToken)
			{
				return Task.FromResult(Result.Success($"Result:{query.Id}"));
			}
		}

		private class TestNotificationHandler : INotificationHandler<TestNotification>
		{
			public bool WasCalled { get; private set; }
			public string? ReceivedMessage { get; private set; }

			public Task Handle(TestNotification notification, CancellationToken cancellationToken)
			{
				WasCalled = true;
				ReceivedMessage = notification.Message;
				return Task.CompletedTask;
			}
		}

		[Test]
		public void RegisterCommandHandler_WithInstance_RegistersHandler()
		{
			TestCommandHandler handler = new();
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterCommandHandler<TestCommand, Guid>(handler);
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterCommandHandler_WithFactory_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterCommandHandler<TestCommand, Guid>(() => new TestCommandHandler());
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterCommandHandler_WithType_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterCommandHandler<TestCommand, Guid, TestCommandHandler>();
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterCommandHandler_WithoutResult_WithInstance_RegistersHandler()
		{
			TestCommandHandlerWithoutResult handler = new();
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterCommandHandler<TestCommandWithoutResult>(handler);
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterCommandHandler_WithoutResult_WithFactory_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterCommandHandler<TestCommandWithoutResult>(() => new TestCommandHandlerWithoutResult());
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterCommandHandler_WithoutResult_WithType_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterCommandHandler<TestCommandWithoutResult, TestCommandHandlerWithoutResult>();
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterQueryHandler_WithInstance_RegistersHandler()
		{
			TestQueryHandler handler = new();
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterQueryHandler<TestQuery, string>(handler);
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterQueryHandler_WithFactory_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterQueryHandler<TestQuery, string>(() => new TestQueryHandler());
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterQueryHandler_WithType_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterQueryHandler<TestQuery, string, TestQueryHandler>();
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void AddNotificationHandler_WithInstance_RegistersHandler()
		{
			TestNotificationHandler handler = new();
			MediatorBuilder builder = new MediatorBuilder()
				.AddNotificationHandler<TestNotification>(handler);
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void AddNotificationHandler_WithFactory_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.AddNotificationHandler<TestNotification>(() => new TestNotificationHandler());
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void AddNotificationHandler_WithType_RegistersHandler()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.AddNotificationHandler<TestNotification, TestNotificationHandler>();
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void Configure_NotificationExecution_SetsOption()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.Configure(options => options.NotificationExecution = NotificationExecution.Parallel);
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterHandlersFromAssembly_RegistersAllHandlers()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterHandlersFromAssembly(typeof(TestCommandHandler).Assembly);
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public void RegisterHandlersFromAssemblies_RegistersAllHandlers()
		{
			MediatorBuilder builder = new MediatorBuilder()
				.RegisterHandlersFromAssemblies(
					typeof(TestCommandHandler).Assembly,
					typeof(MediatorBuilder).Assembly);
			IMediator mediator = builder.Build();

			Assert.That(mediator, Is.Not.Null);
		}

		[Test]
		public async Task Build_WithMultipleRegistrations_ExecutesAll()
		{
			TestCommandHandler commandHandler = new();
			TestQueryHandler queryHandler = new();
			TestNotificationHandler notificationHandler = new();

			MediatorBuilder builder = new MediatorBuilder()
				.RegisterCommandHandler<TestCommand, Guid>(commandHandler)
				.RegisterQueryHandler<TestQuery, string>(queryHandler)
				.AddNotificationHandler<TestNotification>(notificationHandler);

			IMediator mediator = builder.Build();

			Result<Guid> commandResult = await mediator.Send<Guid>(new TestCommand(Guid.NewGuid()));
			Result<string> queryResult = await mediator.Send<string>(new TestQuery("test"));
			await mediator.Publish(new TestNotification("test"));

			Assert.That(commandResult.IsSuccess, Is.True);
			Assert.That(queryResult.IsSuccess, Is.True);
			Assert.That(notificationHandler.WasCalled, Is.True);
		}
	}
}

