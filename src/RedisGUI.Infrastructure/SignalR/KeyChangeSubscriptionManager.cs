using System.Collections.Immutable;

namespace RedisGUI.Infrastructure.SignalR;

/// <summary>
/// Lock-free, thread-safe implementation of key change subscription management.
/// Uses immutable bidirectional indexes swapped atomically via <see cref="Interlocked.CompareExchange{T}"/>.
/// </summary>
public sealed class KeyChangeSubscriptionManager : IKeyChangeSubscriptionManager
{
	private sealed record SubscriptionSnapshot(
		ImmutableDictionary<string, ImmutableHashSet<string>> ConnectionClients,
		ImmutableDictionary<string, ImmutableHashSet<string>> ClientConnections)
	{
		public static readonly SubscriptionSnapshot Empty = new(
			ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
			ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
	}

	private SubscriptionSnapshot snapshot = SubscriptionSnapshot.Empty;

	/// <inheritdoc />
	public void SubscribeToConnection(string clientId, string redisConnectionId)
	{
		ImmutableInterlocked.Update(
			ref snapshot,
			static (current, args) =>
			{
				var (cid, rcid) = args;
				var connClients = current.ConnectionClients.TryGetValue(rcid, out var existingClients)
					? existingClients.Add(cid)
					: [cid];
				var clientConns = current.ClientConnections.TryGetValue(cid, out var existingConns)
					? existingConns.Add(rcid)
					: [rcid];

				return new SubscriptionSnapshot(
					current.ConnectionClients.SetItem(rcid, connClients),
					current.ClientConnections.SetItem(cid, clientConns));
			},
			(clientId, redisConnectionId));
	}

	/// <inheritdoc />
	public void UnsubscribeFromConnection(string clientId, string redisConnectionId)
	{
		ImmutableInterlocked.Update(
			ref snapshot,
			static (current, args) =>
			{
				var (cid, rcid) = args;
				var connectionClients = current.ConnectionClients;
				if (connectionClients.TryGetValue(rcid, out var existingClients))
				{
					var updated = existingClients.Remove(cid);
					connectionClients = updated.IsEmpty
						? connectionClients.Remove(rcid)
						: connectionClients.SetItem(rcid, updated);
				}

				var clientConnections = current.ClientConnections;
				if (clientConnections.TryGetValue(cid, out var existingConns))
				{
					var updated = existingConns.Remove(rcid);
					clientConnections = updated.IsEmpty
						? clientConnections.Remove(cid)
						: clientConnections.SetItem(cid, updated);
				}

				return ReferenceEquals(connectionClients, current.ConnectionClients) &&
					ReferenceEquals(clientConnections, current.ClientConnections)
					? current
					: new SubscriptionSnapshot(connectionClients, clientConnections);
			},
			(clientId, redisConnectionId));
	}

	/// <inheritdoc />
	public void UnsubscribeClient(string clientId)
	{
		ImmutableInterlocked.Update(
			ref snapshot,
			static (current, cid) =>
			{
				if (!current.ClientConnections.TryGetValue(cid, out var subscribedConnections))
				{
					return current;
				}

				var connectionClients = current.ConnectionClients;
				foreach (string redisConnectionId in subscribedConnections)
				{
					if (connectionClients.TryGetValue(redisConnectionId, out var clients))
					{
						var updated = clients.Remove(cid);
						connectionClients = updated.IsEmpty
							? connectionClients.Remove(redisConnectionId)
							: connectionClients.SetItem(redisConnectionId, updated);
					}
				}

				return new SubscriptionSnapshot(
					connectionClients,
					current.ClientConnections.Remove(cid));
			},
			clientId);
	}

	/// <inheritdoc />
	public IEnumerable<string> GetClientsForConnection(string redisConnectionId)
	{
		var current = Volatile.Read(ref snapshot);
		return current.ConnectionClients.TryGetValue(redisConnectionId, out var clients)
			? clients
			: [];
	}
}
