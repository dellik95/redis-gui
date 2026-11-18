using System.Collections.Concurrent;
using System.Security.Claims;

namespace RedisGUI.Infrastructure.BackgroundJobs;

/// <summary>
/// Lock-free registry tracking active SignalR job subscribers and their security principals
/// so access revocation can be enforced before delivering updates.
/// </summary>
public sealed class BackgroundJobSubscriptionRegistry
{
	public sealed record Subscriber(string ConnectionId, ClaimsPrincipal Principal, Guid OwnerId);

	private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, Subscriber>> jobSubscribers = new();
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> connectionJobs = new();

	public void Subscribe(Guid jobId, string connectionId, ClaimsPrincipal principal, Guid ownerId)
	{
		var subscribers = jobSubscribers.GetOrAdd(jobId, _ => new ConcurrentDictionary<string, Subscriber>(StringComparer.Ordinal));
		subscribers[connectionId] = new Subscriber(connectionId, principal, ownerId);

		var jobs = connectionJobs.GetOrAdd(connectionId, _ => new ConcurrentDictionary<Guid, byte>());
		jobs[jobId] = 0;
	}

	public void Unsubscribe(Guid jobId, string connectionId)
	{
		if (jobSubscribers.TryGetValue(jobId, out var subscribers))
		{
			subscribers.TryRemove(connectionId, out _);
			if (subscribers.IsEmpty)
			{
				jobSubscribers.TryRemove(KeyValuePair.Create(jobId, subscribers));
			}
		}

		if (connectionJobs.TryGetValue(connectionId, out var jobs))
		{
			jobs.TryRemove(jobId, out _);
			if (jobs.IsEmpty)
			{
				connectionJobs.TryRemove(KeyValuePair.Create(connectionId, jobs));
			}
		}
	}

	public void RemoveConnection(string connectionId)
	{
		if (!connectionJobs.TryRemove(connectionId, out var jobs))
		{
			return;
		}

		foreach (var jobId in jobs.Keys)
		{
			if (jobSubscribers.TryGetValue(jobId, out var subscribers))
			{
				subscribers.TryRemove(connectionId, out _);
				if (subscribers.IsEmpty)
				{
					jobSubscribers.TryRemove(KeyValuePair.Create(jobId, subscribers));
				}
			}
		}
	}

	public IReadOnlyList<Subscriber> GetSubscribers(Guid jobId) =>
		jobSubscribers.TryGetValue(jobId, out var subscribers)
			? [.. subscribers.Values]
			: [];

	public void RemoveJob(Guid jobId)
	{
		if (!jobSubscribers.TryRemove(jobId, out var subscribers))
		{
			return;
		}

		foreach (var connectionId in subscribers.Keys)
		{
			if (connectionJobs.TryGetValue(connectionId, out var jobs))
			{
				jobs.TryRemove(jobId, out _);
				if (jobs.IsEmpty)
				{
					connectionJobs.TryRemove(KeyValuePair.Create(connectionId, jobs));
				}
			}
		}
	}
}
