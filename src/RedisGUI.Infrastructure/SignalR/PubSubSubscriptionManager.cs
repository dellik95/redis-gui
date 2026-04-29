using System.Collections.Concurrent;
using System.Linq;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Thread-safe implementation of Pub/Sub subscription management
/// </summary>
public sealed class PubSubSubscriptionManager : IPubSubSubscriptionManager
{
	// Maps subscriptionId -> set of clientIds
	private readonly ConcurrentDictionary<string, HashSet<string>> _subscriptionClients = new();

	// Maps clientId -> set of subscriptionIds
	private readonly ConcurrentDictionary<string, HashSet<string>> _clientSubscriptions = new();

	private readonly object _lock = new();

	/// <inheritdoc />
	public void AddClientToSubscription(string clientId, string subscriptionId)
	{
		lock (_lock)
		{
			// Add to subscription -> clients mapping
			if (!_subscriptionClients.TryGetValue(subscriptionId, out HashSet<string>? clients))
			{
				clients = [];
				_subscriptionClients[subscriptionId] = clients;
			}
			clients.Add(clientId);

			// Add to client -> subscriptions mapping
			if (!_clientSubscriptions.TryGetValue(clientId, out HashSet<string>? subscriptions))
			{
				subscriptions = [];
				_clientSubscriptions[clientId] = subscriptions;
			}
			subscriptions.Add(subscriptionId);
		}
	}

	/// <inheritdoc />
	public void RemoveClientFromSubscription(string clientId, string subscriptionId)
	{
		lock (_lock)
		{
			if (_subscriptionClients.TryGetValue(subscriptionId, out HashSet<string>? clients))
			{
				clients.Remove(clientId);
				if (clients.Count == 0)
				{
					_subscriptionClients.TryRemove(subscriptionId, out _);
				}
			}

			if (_clientSubscriptions.TryGetValue(clientId, out HashSet<string>? subscriptions))
			{
				subscriptions.Remove(subscriptionId);
				if (subscriptions.Count == 0)
				{
					_clientSubscriptions.TryRemove(clientId, out _);
				}
			}
		}
	}

	/// <inheritdoc />
	public Task RemoveClientSubscriptions(string clientId, CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			if (_clientSubscriptions.TryRemove(clientId, out HashSet<string>? subscriptions))
			{
				foreach (string subscriptionId in subscriptions)
				{
					if (_subscriptionClients.TryGetValue(subscriptionId, out HashSet<string>? clients))
					{
						clients.Remove(clientId);
						if (clients.Count == 0)
						{
							_subscriptionClients.TryRemove(subscriptionId, out _);
						}
					}
				}
			}
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public IEnumerable<string> GetClientsForSubscription(string subscriptionId)
	{
		lock (_lock)
		{
			if (_subscriptionClients.TryGetValue(subscriptionId, out HashSet<string>? clients))
			{
				return clients.ToList();
			}

			return [];
		}
	}
}

