using RedisGUI.Shared.Mediator;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Tests.Mediator
{
	[TestFixture]
	public class NotificationRegistryTests
	{
		private record TestNotification(string Message) : INotification;

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
		public void AddHandler_WithInstance_RegistersHandler()
		{
			NotificationRegistry registry = new();
			TestNotificationHandler handler = new();

			registry.AddHandler<TestNotification>(handler);

			List<INotificationHandler<TestNotification>> handlers = registry.GetHandlers<TestNotification>().ToList();
			Assert.That(handlers, Has.Count.EqualTo(1));
		}

		[Test]
		public void AddHandler_WithFactory_RegistersHandler()
		{
			NotificationRegistry registry = new();
			TestNotificationHandler handler = new();

			registry.AddHandler<TestNotification>(() => handler);

			List<INotificationHandler<TestNotification>> handlers = registry.GetHandlers<TestNotification>().ToList();
			Assert.That(handlers, Has.Count.EqualTo(1));
		}

		[Test]
		public void AddHandler_WithMultipleHandlers_ReturnsAll()
		{
			NotificationRegistry registry = new();
			TestNotificationHandler handler1 = new();
			TestNotificationHandler handler2 = new();
			TestNotificationHandler handler3 = new();

			registry.AddHandler<TestNotification>(handler1);
			registry.AddHandler<TestNotification>(handler2);
			registry.AddHandler<TestNotification>(handler3);

			List<INotificationHandler<TestNotification>> handlers = registry.GetHandlers<TestNotification>().ToList();
			Assert.That(handlers, Has.Count.EqualTo(3));
		}

		[Test]
		public void GetHandlers_WhenNoHandlersRegistered_ReturnsEmpty()
		{
			NotificationRegistry registry = new();

			List<INotificationHandler<TestNotification>> handlers = registry.GetHandlers<TestNotification>().ToList();
			Assert.That(handlers, Is.Empty);
		}

		[Test]
		public void GetHandlers_ForDifferentNotificationType_ReturnsEmpty()
		{
			NotificationRegistry registry = new();
			TestNotificationHandler handler = new();

			registry.AddHandler<TestNotification>(handler);

			List<INotificationHandler<TestNotification>> handlers = registry.GetHandlers<TestNotification>().ToList();
			Assert.That(handlers, Has.Count.EqualTo(1));
		}

		[Test]
		public async Task GetHandlers_InvokesHandler_Correctly()
		{
			NotificationRegistry registry = new();
			TestNotificationHandler handler = new();
			TestNotification notification = new("test message");

			registry.AddHandler<TestNotification>(handler);

			List<INotificationHandler<TestNotification>> handlers = registry.GetHandlers<TestNotification>().ToList();
			await handlers[0].Handle(notification, CancellationToken.None);

			Assert.That(handler.WasCalled, Is.True);
			Assert.That(handler.ReceivedMessage, Is.EqualTo("test message"));
		}
	}
}

