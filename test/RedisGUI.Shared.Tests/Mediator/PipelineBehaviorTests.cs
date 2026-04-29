using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Tests.Mediator;

[TestFixture]
public class PipelineBehaviorTests
{
	private record TestCommand(string Name) : ICommand<string>;
	private record TestQuery(int Id) : IQuery<int>;

	private class TestCommandHandler : ICommandHandler<TestCommand, string>
	{
		public Task<Result<string>> Handle(TestCommand command, CancellationToken cancellationToken)
		{
			return Task.FromResult(Result.Success($"Processed:{command.Name}"));
		}
	}

	private class TrackingCommandHandler : ICommandHandler<TestCommand, string>
	{
		private readonly List<string> executionOrder;

		public TrackingCommandHandler(List<string> executionOrder)
		{
			this.executionOrder = executionOrder;
		}

		public Task<Result<string>> Handle(TestCommand command, CancellationToken cancellationToken)
		{
			executionOrder.Add("Handler");
			return Task.FromResult(Result.Success($"Processed:{command.Name}"));
		}
	}

	private class TestQueryHandler : IQueryHandler<TestQuery, int>
	{
		public Task<Result<int>> Handle(TestQuery query, CancellationToken cancellationToken)
		{
			return Task.FromResult(Result.Success(query.Id * 2));
		}
	}

	[Test]
	public async Task Send_Command_WithPipelineBehavior_ExecutesBehavior()
	{
		// Arrange
		LoggingBehavior<ICommand<string>, Result<string>> behavior = new();
		TestCommandHandler handler = new();

		MediatorBuilder builder = new MediatorBuilder()
			.RegisterCommandHandler<TestCommand, string>(handler)
			.AddPipelineBehavior<ICommand<string>, Result<string>>(behavior);
		IMediator mediator = builder.Build();

		// Act
		Result<string> result = await mediator.Send<string>(new TestCommand("test"));

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(behavior.WasCalled, Is.True);
		});
	}

	[Test]
	public async Task Send_Command_WithMultipleBehaviors_ExecutesInOrder()
	{
		// Arrange
		List<string> executionOrder = [];
		OrderedBehavior<ICommand<string>, Result<string>> behavior1 = new("Behavior1", executionOrder);
		OrderedBehavior<ICommand<string>, Result<string>> behavior2 = new("Behavior2", executionOrder);
		OrderedBehavior<ICommand<string>, Result<string>> behavior3 = new("Behavior3", executionOrder);
		TrackingCommandHandler handler = new(executionOrder);

		MediatorBuilder builder = new MediatorBuilder()
			.RegisterCommandHandler<TestCommand, string>(handler)
			.AddPipelineBehavior<ICommand<string>, Result<string>>(behavior1)
			.AddPipelineBehavior<ICommand<string>, Result<string>>(behavior2)
			.AddPipelineBehavior<ICommand<string>, Result<string>>(behavior3);
		IMediator mediator = builder.Build();

		// Act
		Result<string> result = await mediator.Send<string>(new TestCommand("test"));

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsSuccess, Is.True);
			// Behaviors are executed in reverse order (last registered is first executed)
			Assert.That(executionOrder, Is.EqualTo(new[] { "Behavior3", "Behavior2", "Behavior1", "Handler" }));
		});
	}

	[Test]
	public async Task Send_Query_WithPipelineBehavior_ExecutesBehavior()
	{
		// Arrange
		LoggingBehavior<IQuery<int>, Result<int>> behavior = new();
		TestQueryHandler handler = new();

		MediatorBuilder builder = new MediatorBuilder()
			.RegisterQueryHandler<TestQuery, int>(handler)
			.AddPipelineBehavior<IQuery<int>, Result<int>>(behavior);
		IMediator mediator = builder.Build();

		// Act
		Result<int> result = await mediator.Send<int>(new TestQuery(5));

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(result.Value, Is.EqualTo(10));
			Assert.That(behavior.WasCalled, Is.True);
		});
	}

	[Test]
	public async Task Send_Command_WithBehaviorThatModifiesRequest_ModifiesRequest()
	{
		// Arrange
		RequestModifyingBehavior<ICommand<string>, Result<string>> behavior = new();
		TestCommandHandler handler = new();

		MediatorBuilder builder = new MediatorBuilder()
			.RegisterCommandHandler<TestCommand, string>(handler)
			.AddPipelineBehavior<ICommand<string>, Result<string>>(behavior);
		IMediator mediator = builder.Build();

		TestCommand command = new("original");

		// Act
		Result<string> result = await mediator.Send<string>(command);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsSuccess, Is.True);
			Assert.That(behavior.ModifiedRequest, Is.Not.Null);
		});
	}

	[Test]
	public async Task Send_Command_WithBehaviorThatReturnsFailure_DoesNotExecuteHandler()
	{
		// Arrange
		FailureBehavior<ICommand<string>, Result<string>> behavior = new();
		TestCommandHandler handler = new();

		MediatorBuilder builder = new MediatorBuilder()
			.RegisterCommandHandler<TestCommand, string>(handler)
			.AddPipelineBehavior<ICommand<string>, Result<string>>(behavior);
		IMediator mediator = builder.Build();

		// Act
		Result<string> result = await mediator.Send<string>(new TestCommand("test"));

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.IsFailure, Is.True);
			Assert.That(result.Error.Code, Is.EqualTo("Behavior.Failure"));
		});
	}

	private class LoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
		where TRequest : IRequest<TResult>
	{
		public bool WasCalled { get; private set; }

		public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next)
		{
			WasCalled = true;
			return await next();
		}
	}

	private class OrderedBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
		where TRequest : IRequest<TResult>
	{
		private readonly string name;
		private readonly List<string> executionOrder;

		public OrderedBehavior(string name, List<string> executionOrder)
		{
			this.name = name;
			this.executionOrder = executionOrder;
		}

		public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next)
		{
			executionOrder.Add(name);
			TResult? result = await next();
			return result;
		}
	}

	private class RequestModifyingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
		where TRequest : IRequest<TResult>
	{
		public TRequest? ModifiedRequest { get; private set; }

		public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next)
		{
			ModifiedRequest = request;
			return await next();
		}
	}

	private class FailureBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
		where TRequest : IRequest<TResult>
	{
		public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next)
		{
			// Return failure without calling next
			if (typeof(TResult) == typeof(Result))
			{
				return (TResult)(object)Result.Failure(new Error("Behavior.Failure", "Behavior prevented execution"));
			}

			if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
			{
				Type innerType = typeof(TResult).GetGenericArguments()[0];
				MethodInfo failureMethod = typeof(Result)
					.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
					.First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
					.MakeGenericMethod(innerType);
				return (TResult)failureMethod.Invoke(null, [new Error("Behavior.Failure", "Behavior prevented execution")])!;
			}

			return await next();
		}
	}
}

