using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedisGUI.Application.Core.Services;
using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.BackgroundJobs;

/// <summary>Observable state for an in-process job.</summary>
public sealed record BackgroundJobSnapshot(Guid JobId, Guid ConnectionId, string Kind, string Status, long Revision,
	double? ProgressPercent, object? Metrics, string? Error, bool HasResult, DateTime UpdatedAt);

/// <summary>Bounded, in-process background job queue with per-user ownership.</summary>
public sealed class BackgroundJobManager(IServiceScopeFactory scopes, IHubContext<BackgroundJobsHub> hub,
	ILogger<BackgroundJobManager> logger) : BackgroundService
{
	private const int MaxJobs = 512;
	private const long MaxActiveParameterBytes = 64L * 1024 * 1024;
	private long activeParameterBytes;
	private readonly ConcurrentDictionary<Guid, Job> jobs = new();
	private readonly Channel<Job> queue = Channel.CreateBounded<Job>(new BoundedChannelOptions(100)
	{
		FullMode = BoundedChannelFullMode.Wait, SingleReader = false, SingleWriter = false
	});
	private readonly object admission = new();

	public bool TryStart(Guid ownerId, ClaimsPrincipal principal, Guid connectionId, string kind, JsonElement parameters, out BackgroundJobSnapshot? snapshot)
	{
		lock (admission)
		{
			RemoveExpired();
			long payloadBytes = (long)parameters.GetRawText().Length * sizeof(char);
			if (jobs.Count >= MaxJobs || payloadBytes > MaxActiveParameterBytes - activeParameterBytes) { snapshot = null; return false; }
			var identity = new ClaimsPrincipal(principal.Identities.Select(identity => new ClaimsIdentity(identity)));
			var job = new Job(Guid.NewGuid(), ownerId, identity, connectionId, kind, parameters.Clone());
			if (!jobs.TryAdd(job.Id, job) || !queue.Writer.TryWrite(job))
			{
				jobs.TryRemove(job.Id, out _);
				job.Cancellation.Dispose();
				snapshot = null;
				return false;
			}
			activeParameterBytes += payloadBytes;
			job.ParameterBytes = payloadBytes;
			snapshot = job.Snapshot();
			return true;
		}
	}

	public BackgroundJobSnapshot? Get(Guid jobId, Guid ownerId) =>
		jobs.TryGetValue(jobId, out var job) && job.OwnerId == ownerId ? job.Snapshot() : null;

	public object? GetResult(Guid jobId, Guid ownerId) =>
		jobs.TryGetValue(jobId, out var job) && job.OwnerId == ownerId ? job.Result : null;

	public async Task<bool> CancelAsync(Guid jobId, Guid ownerId)
	{
		if (!jobs.TryGetValue(jobId, out var job) || job.OwnerId != ownerId) return false;
		var changed = job.Cancel();
		if (changed is not null) await PublishAsync(changed);
		return true;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try { await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => RunWorkerAsync(stoppingToken)).Append(CleanupAsync(stoppingToken))); }
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await base.StopAsync(cancellationToken);
		foreach (var job in jobs.Values)
		{
			try { if (job.Result is BackgroundJobFile file) File.Delete(file.Path); }
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{ logger.LogWarning(ex, "Could not remove job file {JobId} during shutdown", job.Id); }
		}
	}

	private async Task CleanupAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			lock (admission) RemoveExpired();
		}
	}

	private void RemoveExpired()
	{
		foreach (var entry in jobs)
		{
			var current = entry.Value.Snapshot();
			if (!IsFinished(current.Status) || current.UpdatedAt >= DateTime.UtcNow.AddMinutes(-30)) continue;
			try
			{
				if (entry.Value.Result is BackgroundJobFile file) File.Delete(file.Path);
				if (jobs.TryRemove(entry.Key, out var expired)) expired.Cancellation.Dispose();
			}
			catch (IOException ex) { logger.LogWarning(ex, "Could not remove expired job file {JobId}; will retry", entry.Key); }
			catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Could not remove expired job file {JobId}; will retry", entry.Key); }
		}
	}

	private async Task RunWorkerAsync(CancellationToken stoppingToken)
	{
		await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
		{
			using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.Cancellation.Token);
			try
			{
				linked.Token.ThrowIfCancellationRequested();
				await PublishAsync(job.Update("Running"));
				using var scope = scopes.CreateScope();
				scope.ServiceProvider.GetRequiredService<JobIdentity>().Principal = job.Principal;
				var handler = scope.ServiceProvider.GetServices<IBackgroundJobHandler>().SingleOrDefault(x => x.Kind == job.Kind)
					?? throw new InvalidOperationException("Job handler is unavailable.");
				var result = await handler.ExecuteAsync(job.ConnectionId, job.Parameters, async (percent, metrics) =>
				{
					if (!linked.IsCancellationRequested) await PublishAsync(job.Progress(percent, metrics));
				}, linked.Token);
				if (linked.IsCancellationRequested) await PublishAsync(job.Cancelled(result.IsSuccess ? result.Value : null));
				else if (result.IsFailure) await PublishAsync(job.Update("Failed", result.Error.Message));
				else await PublishAsync(job.Complete(result.Value));
			}
			catch (OperationCanceledException) when (linked.IsCancellationRequested) { await PublishAsync(job.Update("Cancelled")); }
			catch (Exception ex)
			{
				logger.LogError(ex, "Background job {JobId} failed", job.Id);
				await PublishAsync(job.Update("Failed", "The operation failed. Check the server logs."));
			}
			finally
			{
				lock (admission) { activeParameterBytes -= job.ParameterBytes; job.ReleaseParameters(); }
			}
		}
	}

	private async Task PublishAsync(BackgroundJobSnapshot snapshot)
	{
		try
		{
			await hub.Clients.Group(BackgroundJobsHub.Group(snapshot.JobId)).SendAsync("JobUpdated", snapshot);
			if (IsFinished(snapshot.Status)) await hub.Clients.Group(BackgroundJobsHub.Group(snapshot.JobId)).SendAsync("JobCompleted", snapshot);
		}
		catch (Exception ex) { logger.LogWarning(ex, "Could not publish job update {JobId}", snapshot.JobId); }
	}

	private static bool IsFinished(string status) => status is "Completed" or "Failed" or "Cancelled";

	private sealed class Job(Guid id, Guid ownerId, ClaimsPrincipal principal, Guid connectionId, string kind, JsonElement parameters)
	{
		private readonly object gate = new();
		private string status = "Queued";
		private long revision;
		private double? progress;
		private object? metrics;
		private string? error;
		private object? result;
		private DateTime updatedAt = DateTime.UtcNow;
		public Guid Id { get; } = id;
		public Guid OwnerId { get; } = ownerId;
		public ClaimsPrincipal Principal { get; } = principal;
		public Guid ConnectionId { get; } = connectionId;
		public string Kind { get; } = kind;
		public JsonElement Parameters { get; private set; } = parameters;
		public long ParameterBytes { get; set; }
		public void ReleaseParameters() => Parameters = default;
		public CancellationTokenSource Cancellation { get; } = new();
		public object? Result { get { lock (gate) return IsFinished(status) ? result : null; } }
		public BackgroundJobSnapshot Snapshot() { lock (gate) return Current(); }
		public BackgroundJobSnapshot Update(string next, string? failure = null)
		{
			lock (gate) { if (next != "Running" || status != "Cancelling") status = next; error = failure; revision++; updatedAt = DateTime.UtcNow; return Current(); }
		}
		public BackgroundJobSnapshot Progress(double? percent, object? value)
		{
			lock (gate) { if (status != "Running") return Current(); progress = percent.HasValue && double.IsFinite(percent.Value) ? Math.Clamp(percent.Value, 0, 100) : null; metrics = value; revision++; updatedAt = DateTime.UtcNow; return Current(); }
		}
		public BackgroundJobSnapshot Complete(object? value)
		{
			lock (gate) { result = value; status = status == "Cancelling" ? "Cancelled" : "Completed"; if (status == "Completed") progress = 100; revision++; updatedAt = DateTime.UtcNow; return Current(); }
		}
		public BackgroundJobSnapshot Cancelled(object? value)
		{
			lock (gate) { result = value; status = "Cancelled"; revision++; updatedAt = DateTime.UtcNow; return Current(); }
		}
		public BackgroundJobSnapshot? Cancel()
		{
			BackgroundJobSnapshot snapshot;
			lock (gate)
			{
				if (IsFinished(status) || status == "Cancelling") return null;
				status = "Cancelling"; revision++; updatedAt = DateTime.UtcNow;
				snapshot = Current();
			}
			try { Cancellation.Cancel(); } catch (ObjectDisposedException) { }
			return snapshot;
		}
		private BackgroundJobSnapshot Current() => new(Id, ConnectionId, Kind, status, revision, progress, metrics, error, IsFinished(status) && result is not null, updatedAt);
	}
}
