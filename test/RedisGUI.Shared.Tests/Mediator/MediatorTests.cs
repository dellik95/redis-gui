using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Shared.Tests.Mediator
{
    public class MediatorTests
    {
        private record TestCommand(Guid Seed) : ICommand<Guid>;

        private class TestCommandHandler : ICommandHandler<TestCommand, Guid>
        {
            public Task<Result<Guid>> Handle(TestCommand command, CancellationToken cancellationToken)
            {
                return Task.FromResult(Result.Success(Guid.NewGuid()));
            }
        }

        [Test]
        public async Task Send_Command_Returns_Result()
        {
            var command = new TestCommand(Guid.Empty);
            var builder = new MediatorBuilder()
                .RegisterCommandHandler<TestCommand, Guid>(new TestCommandHandler());
            var mediator = builder.Build();

            var result = await mediator.Send<Guid>(command, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.EqualTo(Guid.Empty));
        }

        private record TestQuery(string Id) : IQuery<string>;

        private class TestQueryHandler : IQueryHandler<TestQuery, string>
        {
            public Task<Result<string>> Handle(TestQuery query, CancellationToken cancellationToken)
            {
                return Task.FromResult(Result.Success($"User:{query.Id}"));
            }
        }

        [Test]
        public async Task Send_Query_Returns_Result()
        {
            var query = new TestQuery("123");
            var builder = new MediatorBuilder()
                .RegisterQueryHandler<TestQuery, string>(new TestQueryHandler());
            var mediator = builder.Build();

            var result = await mediator.Send<string>(query, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Does.StartWith("User:"));
        }

        private record TestNotification(string Message) : INotification;

        private class TestNotificationHandler : INotificationHandler<TestNotification>
        {
            public bool WasCalled { get; private set; }
            public Task Handle(TestNotification notification, CancellationToken cancellationToken)
            {
                WasCalled = true;
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task Publish_Notification_Invokes_Handler()
        {
            var notification = new TestNotification("Hello");
            var handler = new TestNotificationHandler();
            var builder = new MediatorBuilder()
                .AddNotificationHandler<TestNotification>(handler);
            var mediator = builder.Build();

            await mediator.Publish(notification, CancellationToken.None);

            Assert.That(handler.WasCalled, Is.True);
        }
    }
}
