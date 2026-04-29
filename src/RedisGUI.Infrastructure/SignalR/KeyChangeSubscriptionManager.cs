using System.Collections.Concurrent;
using System.Linq;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Thread-safe implementation of key change subscription management
/// </summary>
public sealed class KeyChangeSubscriptionManager : IKeyChangeSubscriptionManager
{
	// Maps redisConnectionId -> set of clientIds
	private readonly ConcurrentDictionary<string, HashSet<string>> _connectionClients = new();

	// Maps clientId -> set of redisConnectionIds
	private readonly ConcurrentDictionary<string, HashSet<string>> _clientConnections = new();

	private readonly object _lock = new();

	/// <inheritdoc />
	public void SubscribeToConnection(string clientId, string redisConnectionId)
	{
		lock (_lock)
		{
			// Add to connection -> clients mapping
			if (!_connectionClients.TryGetValue(redisConnectionId, out HashSet<string>? clients))
			{
				clients = [];
				_connectionClients[redisConnectionId] = clients;
			}
			clients.Add(clientId);

			// Add to client -> connections mapping
			if (!_clientConnections.TryGetValue(clientId, out HashSet<string>? connections))
			{
				connections = [];
				_clientConnections[clientId] = connections;
			}
			connections.Add(redisConnectionId);
		}
	}

	/// <inheritdoc />
	public void UnsubscribeFromConnection(string clientId, string redisConnectionId)
	{
		lock (_lock)
		{
			if (_connectionClients.TryGetValue(redisConnectionId, out HashSet<string>? clients))
			{
				clients.Remove(clientId);
				if (clients.Count == 0)
				{
					_connectionClients.TryRemove(redisConnectionId, out _);
				}
			}

			if (_clientConnections.TryGetValue(clientId, out HashSet<string>? connections))
			{
				connections.Remove(redisConnectionId);
				if (connections.Count == 0)
				{
					_clientConnections.TryRemove(clientId, out _);
				}
			}
		}
	}

	/// <inheritdoc />
	public void UnsubscribeClient(string clientId)
	{
		lock (_lock)
		{
			if (_clientConnections.TryRemove(clientId, out HashSet<string>? connections))
			{
				foreach (string redisConnectionId in connections)
				{
					if (_connectionClients.TryGetValue(redisConnectionId, out HashSet<string>? clients))
					{
						clients.Remove(clientId);
						if (clients.Count == 0)
						{
							_connectionClients.TryRemove(redisConnectionId, out _);
						}
					}
				}
			}
		}
	}

	/// <inheritdoc />
	public IEnumerable<string> GetClientsForConnection(string redisConnectionId)
	{
		lock (_lock)
		{
			if (_connectionClients.TryGetValue(redisConnectionId, out HashSet<string>? clients))
			{
				return clients.ToList();
			}

			return [];
		}
	}
}

