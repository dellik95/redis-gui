using System.Threading;
using System.Threading.Tasks;
namespace RedisGUI.Shared.Mediator;

public sealed class IsolatedNotificationPublisher(MediatorOptions options) : DispatchWeave.INotificationPublisher
{
	public async Task Publish<T>(IEnumerable<DispatchWeave.INotificationHandler<T>> handlers, T notification, CancellationToken token)
		where T : DispatchWeave.INotification
	{
		async Task Invoke(DispatchWeave.INotificationHandler<T> handler)
		{
			try { await handler.Handle(notification, token); }
			catch { /* Redis GUI isolates each subscriber's failure. */ }
		}

		if (options.NotificationExecution == NotificationExecution.Parallel)
			await Task.WhenAll(handlers.Select(Invoke));
		else
			foreach (var handler in handlers)
				await Invoke(handler);
	}
}
