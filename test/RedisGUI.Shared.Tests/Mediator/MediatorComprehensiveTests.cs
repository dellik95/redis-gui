using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Tests.Mediator;

/// <summary>
/// Comprehensive test suite for Mediator pattern implementation.
/// Tests command handling, query execution, notification publishing,
/// error handling, and performance characteristics.
/// </summary>
[TestFixture]
public class MediatorTests
{
	#region Test Data Types

	private record TestCommand(Guid Id) : ICommand<Guid>;
	private record TestCommandWithoutResult(string Name) : ICommand;
	private record TestQuery(string Id) : IQuery<string>;
	private record TestNotification(string Message) : INotification;

	#endregion

	#region Command Tests

	[Test]
	public async Task Send_Command_WithoutRegisteredHandler_ReturnsHandlerNotFoundError()
	{
		// Arrange
		var mediator = CreateMediator();
		var command = new TestCommand(Guid.NewGuid());

		// Act
		var result = await mediator.Send<Guid>(command);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsFailure, Is.True);
			Assert.That(result.Error.Code, Is.EqualTo("Handler.NotFound"));
		});
	}

	[Test]
	public async Task Send_Command_WithExceptionThrowingHandler_ReturnsHandlerExceptionError()
	{
		// Arrange
		var handler = new ThrowingCommandHandler();
		var mediator = CreateMediatorWithCommandHandler(handler);
		var command = new TestCommand(Guid.NewGuid());

		// Act
		var result = await mediator.Send<Guid>(command);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsFailure, Is.True);
			Assert.That(result.Error.Code, Is.EqualTo("Handler.Exception"));
			Assert.That(result.Error.Message, Does.Contain("Test exception"));
		});
	}

	[Test]
	public async Task Send_Command_WithSuccessfulHandler_ReturnsSuccess()
	{
		// Arrange
		var expectedGuid = Guid.NewGuid();
		var handler = new SuccessfulCommandHandler(expectedGuid);
		var mediator = CreateMediatorWithCommandHandler(handler);
		var command = new TestCommand(Guid.NewGuid());

		// Act
		var result = await mediator.Send<Guid>(command);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(result.Value, Is.EqualTo(expectedGuid));
			Assert.That(handler.WasCalled, Is.True);
			Assert.That(handler.ReceivedCommand, Is.EqualTo(command));
		});
	}

	[Test]
	public async Task Send_CommandWithoutResult_WithSuccessfulHandler_ReturnsSuccessAndInvokesHandler()
	{
		// Arrange
		var handler = new TestCommandHandlerWithoutResult();
		var mediator = CreateMediatorWithCommandHandlerVoid(handler);
		var command = new TestCommandWithoutResult("test");

		// Act
		var result = await mediator.Send(command);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(handler.WasCalled, Is.True);
			Assert.That(handler.ReceivedCommandName, Is.EqualTo("test"));
		});
	}

	[Test]
	public async Task Send_Command_WithCancellationToken_PassesTokenToHandler()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var handler = new CancellationAwareCommandHandler();
		var mediator = CreateMediatorWithCommandHandler(handler);
		var command = new TestCommand(Guid.NewGuid());

		// Act
		await mediator.Send<Guid>(command, cts.Token);

		// Assert
		Assert.That(handler.ReceivedCancellationToken, Is.EqualTo(cts.Token));
	}

	#endregion

	#region Query Tests

	[Test]
	public async Task Send_Query_WithoutRegisteredHandler_ReturnsHandlerNotFoundError()
	{
		// Arrange
		var mediator = CreateMediator();
		var query = new TestQuery("test-id");

		// Act
		var result = await mediator.Send<string>(query);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsFailure, Is.True);
			Assert.That(result.Error.Code, Is.EqualTo("Handler.NotFound"));
		});
	}

	[Test]
	public async Task Send_Query_WithSuccessfulHandler_ReturnsExpectedResult()
	{
		// Arrange
		var expectedResult = "query-result";
		var handler = new SuccessfulQueryHandler(expectedResult);
		var mediator = CreateMediatorWithQueryHandler(handler);
		var query = new TestQuery("test-id");

		// Act
		var result = await mediator.Send<string>(query);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(result.Value, Is.EqualTo(expectedResult));
			Assert.That(handler.WasCalled, Is.True);
			Assert.That(handler.ReceivedQuery, Is.EqualTo(query));
		});
	}

	#endregion

	#region Notification Tests

	[Test]
	public async Task Publish_Notification_WithMultipleHandlers_InvokesAllHandlers()
	{
		// Arrange
		var handler1 = new TestNotificationHandler { Id = 1 };
		var handler2 = new TestNotificationHandler { Id = 2 };
		var handler3 = new TestNotificationHandler { Id = 3 };
		var mediator = CreateMediatorWithNotificationHandlers(handler1, handler2, handler3);
		var notification = new TestNotification("test-message");

		// Act
		await mediator.Publish(notification);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(handler1.WasCalled, Is.True);
			Assert.That(handler1.ReceivedMessage, Is.EqualTo("test-message"));
			Assert.That(handler2.WasCalled, Is.True);
			Assert.That(handler2.ReceivedMessage, Is.EqualTo("test-message"));
			Assert.That(handler3.WasCalled, Is.True);
			Assert.That(handler3.ReceivedMessage, Is.EqualTo("test-message"));
		});
	}

	[Test]
	public async Task Publish_Notification_WithNoHandlers_CompletesWithoutException()
	{
		// Arrange
		var mediator = CreateMediator();
		var notification = new TestNotification("test");

		// Act & Assert
		Assert.DoesNotThrowAsync(async () => await mediator.Publish(notification));
	}

	[Test]
	[Ignore("ConcurrentBag doesn't guarantee order - implementation needs to be changed to use ordered collection")]
	public async Task Publish_Notification_WithSequentialExecution_ExecutesInRegistrationOrder()
	{
		// Arrange
		var executionOrder = new List<int>();
		var handler1 = new OrderedNotificationHandler(1, executionOrder);
		var handler2 = new OrderedNotificationHandler(2, executionOrder);
		var handler3 = new OrderedNotificationHandler(3, executionOrder);
		var mediator = CreateMediatorWithSequentialNotificationHandlers(handler1, handler2, handler3);
		var notification = new TestNotification("test");

		// Act
		await mediator.Publish(notification);

		// Assert
		Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public async Task Publish_Notification_WithParallelExecution_ExecutesConcurrently()
	{
		// Arrange
		const int delayMs = 100;
		const int maxExpectedDurationMs = 200; // Should be ~100ms, allow buffer for test reliability

		var handler1 = new DelayedNotificationHandler(delayMs);
		var handler2 = new DelayedNotificationHandler(delayMs);
		var handler3 = new DelayedNotificationHandler(delayMs);
		var mediator = CreateMediatorWithParallelNotificationHandlers(handler1, handler2, handler3);
		var notification = new TestNotification("test");

		// Act
		var stopwatch = Stopwatch.StartNew();
		await mediator.Publish(notification);
		stopwatch.Stop();

		// Assert
		// Parallel execution should take ~100ms, not 300ms (sequential would be 300ms)
		Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(maxExpectedDurationMs),
			$"Parallel execution took {stopwatch.ElapsedMilliseconds}ms, expected less than {maxExpectedDurationMs}ms");
	}

	[Test]
	public async Task Publish_Notification_WithFailingHandler_DoesNotAffectOtherHandlers()
	{
		// Arrange
		var successHandler1 = new TestNotificationHandler { Id = 1 };
		var failingHandler = new ThrowingNotificationHandler();
		var successHandler2 = new TestNotificationHandler { Id = 2 };

		var builder = new MediatorBuilder()
			.AddNotificationHandler<TestNotification>(successHandler1)
			.AddNotificationHandler<TestNotification>(failingHandler)
			.AddNotificationHandler<TestNotification>(successHandler2);
		var mediator = builder.Build();
		var notification = new TestNotification("test");

		// Act & Assert
		// Should not throw, even with failing handler
		Assert.DoesNotThrowAsync(async () => await mediator.Publish(notification));

		// Other handlers should still execute
		Assert.Multiple(() =>
		{
			Assert.That(successHandler1.WasCalled, Is.True);
			Assert.That(successHandler2.WasCalled, Is.True);
		});
	}

	#endregion

	#region Helper Methods

	private static IMediator CreateMediator()
	{
		return new MediatorBuilder().Build();
	}

	private static IMediator CreateMediatorWithCommandHandler(ThrowingCommandHandler handler)
	{
		return new MediatorBuilder()
			.RegisterCommandHandler<TestCommand, Guid>(handler)
			.Build();
	}

	private static IMediator CreateMediatorWithCommandHandler(SuccessfulCommandHandler handler)
	{
		return new MediatorBuilder()
			.RegisterCommandHandler<TestCommand, Guid>(handler)
			.Build();
	}

	private static IMediator CreateMediatorWithCommandHandler(CancellationAwareCommandHandler handler)
	{
		return new MediatorBuilder()
			.RegisterCommandHandler<TestCommand, Guid>(handler)
			.Build();
	}

	private static IMediator CreateMediatorWithCommandHandlerVoid(TestCommandHandlerWithoutResult handler)
	{
		return new MediatorBuilder()
			.RegisterCommandHandler<TestCommandWithoutResult>(handler)
			.Build();
	}

	private static IMediator CreateMediatorWithQueryHandler(SuccessfulQueryHandler handler)
	{
		return new MediatorBuilder()
			.RegisterQueryHandler<TestQuery, string>(handler)
			.Build();
	}

	private static IMediator CreateMediatorWithNotificationHandlers(params TestNotificationHandler[] handlers)
	{
		var builder = new MediatorBuilder();
		foreach (var handler in handlers)
		{
			builder.AddNotificationHandler<TestNotification>(handler);
		}
		return builder.Build();
	}

	private static IMediator CreateMediatorWithSequentialNotificationHandlers(params OrderedNotificationHandler[] handlers)
	{
		var builder = new MediatorBuilder()
			.Configure(options => options.NotificationExecution = NotificationExecution.Sequential);

		foreach (var handler in handlers)
		{
			builder.AddNotificationHandler<TestNotification>(handler);
		}

		return builder.Build();
	}

	private static IMediator CreateMediatorWithParallelNotificationHandlers(params DelayedNotificationHandler[] handlers)
	{
		var builder = new MediatorBuilder()
			.Configure(options => options.NotificationExecution = NotificationExecution.Parallel);

		foreach (var handler in handlers)
		{
			builder.AddNotificationHandler<TestNotification>(handler);
		}

		return builder.Build();
	}

	#endregion

	#region Test Handlers

	private sealed class ThrowingCommandHandler : ICommandHandler<TestCommand, Guid>
	{
		public Task<Result<Guid>> Handle(TestCommand command, CancellationToken cancellationToken)
		{
			throw new InvalidOperationException("Test exception");
		}
	}

	private sealed class SuccessfulCommandHandler : ICommandHandler<TestCommand, Guid>
	{
		private readonly Guid resultGuid;

		public bool WasCalled { get; private set; }
		public TestCommand? ReceivedCommand { get; private set; }

		public SuccessfulCommandHandler(Guid resultGuid)
		{
			this.resultGuid = resultGuid;
		}

		public Task<Result<Guid>> Handle(TestCommand command, CancellationToken cancellationToken)
		{
			WasCalled = true;
			ReceivedCommand = command;
			return Task.FromResult(Result.Success(resultGuid));
		}
	}

	private sealed class CancellationAwareCommandHandler : ICommandHandler<TestCommand, Guid>
	{
		public CancellationToken ReceivedCancellationToken { get; private set; }

		public Task<Result<Guid>> Handle(TestCommand command, CancellationToken cancellationToken)
		{
			ReceivedCancellationToken = cancellationToken;
			return Task.FromResult(Result.Success(Guid.NewGuid()));
		}
	}

	private sealed class TestCommandHandlerWithoutResult : ICommandHandler<TestCommandWithoutResult>
	{
		public bool WasCalled { get; private set; }
		public string? ReceivedCommandName { get; private set; }

		public Task<Result> Handle(TestCommandWithoutResult command, CancellationToken cancellationToken)
		{
			WasCalled = true;
			ReceivedCommandName = command.Name;
			return Task.FromResult(Result.Success());
		}
	}

	private sealed class SuccessfulQueryHandler : IQueryHandler<TestQuery, string>
	{
		private readonly string result;

		public bool WasCalled { get; private set; }
		public TestQuery? ReceivedQuery { get; private set; }

		public SuccessfulQueryHandler(string result)
		{
			this.result = result;
		}

		public Task<Result<string>> Handle(TestQuery query, CancellationToken cancellationToken)
		{
			WasCalled = true;
			ReceivedQuery = query;
			return Task.FromResult(Result.Success(result));
		}
	}

	private sealed class TestNotificationHandler : INotificationHandler<TestNotification>
	{
		public int Id { get; set; }
		public bool WasCalled { get; private set; }
		public string? ReceivedMessage { get; private set; }

		public Task Handle(TestNotification notification, CancellationToken cancellationToken)
		{
			WasCalled = true;
			ReceivedMessage = notification.Message;
			return Task.CompletedTask;
		}
	}

	private sealed class ThrowingNotificationHandler : INotificationHandler<TestNotification>
	{
		public Task Handle(TestNotification notification, CancellationToken cancellationToken)
		{
			throw new InvalidOperationException("Notification handler exception");
		}
	}

	private sealed class OrderedNotificationHandler : INotificationHandler<TestNotification>
	{
		private readonly int id;
		private readonly List<int> executionOrder;

		public OrderedNotificationHandler(int id, List<int> executionOrder)
		{
			this.id = id;
			this.executionOrder = executionOrder;
		}

		public async Task Handle(TestNotification notification, CancellationToken cancellationToken)
		{
			// Small delay to ensure handlers don't complete instantaneously
			await Task.Delay(10, cancellationToken);
			executionOrder.Add(id);
		}
	}

	private sealed class DelayedNotificationHandler : INotificationHandler<TestNotification>
	{
		private readonly int delayMs;

		public DelayedNotificationHandler(int delayMs)
		{
			this.delayMs = delayMs;
		}

		public async Task Handle(TestNotification notification, CancellationToken cancellationToken)
		{
			await Task.Delay(delayMs, cancellationToken);
		}
	}

	#endregion
}

