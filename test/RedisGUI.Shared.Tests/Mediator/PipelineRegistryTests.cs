using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Tests.Mediator
{
	[TestFixture]
	public class PipelineRegistryTests
	{
		private record TestCommand(string Name) : ICommand<string>;
		private record TestQuery(int Id) : IQuery<int>;

		private class TestBehavior : IPipelineBehavior<TestCommand, Result<string>>
		{
			public Task<Result<string>> Handle(TestCommand request, CancellationToken cancellationToken, Func<Task<Result<string>>> next)
			{
				return next();
			}
		}

		[Test]
		public void AddBehavior_WithInstance_RegistersBehavior()
		{
			PipelineRegistry registry = new();
			TestBehavior behavior = new();

			registry.AddBehavior<TestCommand, Result<string>>(behavior);

			List<IPipelineBehavior<TestCommand, Result<string>>> behaviors = registry.GetBehaviors<TestCommand, Result<string>>().ToList();
			Assert.That(behaviors, Has.Count.EqualTo(1));
		}

		[Test]
		public void AddBehavior_WithFactory_RegistersBehavior()
		{
			PipelineRegistry registry = new();
			TestBehavior behavior = new();

			registry.AddBehavior<TestCommand, Result<string>>(() => behavior);

			List<IPipelineBehavior<TestCommand, Result<string>>> behaviors = registry.GetBehaviors<TestCommand, Result<string>>().ToList();
			Assert.That(behaviors, Has.Count.EqualTo(1));
		}

		[Test]
		public void AddBehavior_WithMultipleBehaviors_ReturnsAll()
		{
			PipelineRegistry registry = new();
			TestBehavior behavior1 = new();
			TestBehavior behavior2 = new();
			TestBehavior behavior3 = new();

			registry.AddBehavior<TestCommand, Result<string>>(behavior1);
			registry.AddBehavior<TestCommand, Result<string>>(behavior2);
			registry.AddBehavior<TestCommand, Result<string>>(behavior3);

			List<IPipelineBehavior<TestCommand, Result<string>>> behaviors = registry.GetBehaviors<TestCommand, Result<string>>().ToList();
			Assert.That(behaviors, Has.Count.EqualTo(3));
		}

		[Test]
		public void GetBehaviors_WhenNoBehaviorsRegistered_ReturnsEmpty()
		{
			PipelineRegistry registry = new();

			List<IPipelineBehavior<TestCommand, Result<string>>> behaviors = registry.GetBehaviors<TestCommand, Result<string>>().ToList();
			Assert.That(behaviors, Is.Empty);
		}

		[Test]
		public void GetBehaviors_ForDifferentRequestType_ReturnsEmpty()
		{
			PipelineRegistry registry = new();
			TestBehavior behavior = new();

			registry.AddBehavior<TestCommand, Result<string>>(behavior);

			List<IPipelineBehavior<TestQuery, Result<int>>> behaviors = registry.GetBehaviors<TestQuery, Result<int>>().ToList();
			Assert.That(behaviors, Is.Empty);
		}
	}
}

