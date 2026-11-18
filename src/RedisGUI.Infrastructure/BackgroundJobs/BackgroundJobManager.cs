using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RedisGUI.Application.Core.Services;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.SignalR.Hubs;

namespace RedisGUI.Infrastructure.BackgroundJobs;

/// <summary>Observable state for an in-process job.</summary>
public sealed record BackgroundJobSnapshot(
	Guid JobId,
	Guid ConnectionId,
	string Kind,
	string Status,
	long Revision,
	double? ProgressPercent,
	object? Metrics,
	string? Error,
	bool HasResult,
	DateTime UpdatedAt,
	DateTime? CreatedAt = null,
	DateTime? StartedAt = null,
	DateTime? CompletedAt = null,
	bool CancellationRequested = false,
	bool MayHaveUnconfirmedChanges = false,
	string? DisplayName = null);

/// <summary>
/// Lock-free, bounded, in-process background job manager with per-user ownership,
/// deterministic cancellation/failure precedence, and ordered SignalR notification delivery.
/// </summary>
public sealed class BackgroundJobManager : BackgroundService
{
	private static readonly Meter JobsMeter = new("RedisGUI.BackgroundJobs");
	private static readonly Counter<long> StartedCounter = JobsMeter.CreateCounter<long>("redisgui.jobs.started");
	private static readonly Counter<long> RejectedCounter = JobsMeter.CreateCounter<long>("redisgui.jobs.rejected");
	private static readonly Counter<long> CompletedCounter = JobsMeter.CreateCounter<long>("redisgui.jobs.completed");
	private static readonly Counter<long> FailedCounter = JobsMeter.CreateCounter<long>("redisgui.jobs.failed");
	private static readonly Counter<long> CancelledCounter = JobsMeter.CreateCounter<long>("redisgui.jobs.cancelled");

	private readonly IServiceScopeFactory scopes;
	private readonly IHubContext<BackgroundJobsHub> hub;
	private readonly ILogger<BackgroundJobManager> logger;
	private readonly BackgroundJobOptions options;
	private readonly TimeProvider timeProvider;
	private readonly BackgroundJobSubscriptionRegistry? subscriptionRegistry;

	private readonly ConcurrentDictionary<Guid, Job> jobs = new();
	private readonly Channel<Job> queue;
	private long activeParameterBytes;
	private int retainedJobCount;

	public BackgroundJobManager(
		IServiceScopeFactory scopes,
		IHubContext<BackgroundJobsHub> hub,
		ILogger<BackgroundJobManager> logger,
		IOptions<BackgroundJobOptions>? options = null,
		TimeProvider? timeProvider = null,
		BackgroundJobSubscriptionRegistry? subscriptionRegistry = null)
	{
		this.scopes = scopes;
		this.hub = hub;
		this.logger = logger;
		this.options = options?.Value ?? new BackgroundJobOptions();
		this.timeProvider = timeProvider ?? TimeProvider.System;
		this.subscriptionRegistry = subscriptionRegistry;

		queue = Channel.CreateBounded<Job>(new BoundedChannelOptions(this.options.QueueCapacity)
		{
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = false,
			SingleWriter = false
		});
	}

	public bool TryStart(
		Guid ownerId,
		ClaimsPrincipal principal,
		Guid connectionId,
		string kind,
		JsonElement parameters,
		out BackgroundJobSnapshot? snapshot,
		string? displayName = null)
	{
		RemoveExpired();

		long payloadBytes = (long)parameters.GetRawText().Length * sizeof(char);
		if (!TryReserveJobSlot())
		{
			RejectedCounter.Add(1, new KeyValuePair<string, object?>("reason", "max_jobs"));
			snapshot = null;
			return false;
		}

		if (!TryReserveParameterBytes(payloadBytes))
		{
			Interlocked.Decrement(ref retainedJobCount);
			RejectedCounter.Add(1, new KeyValuePair<string, object?>("reason", "memory_budget"));
			snapshot = null;
			return false;
		}

		var identity = new ClaimsPrincipal(principal.Identities.Select(i => new ClaimsIdentity(i)));
		var now = timeProvider.GetUtcNow().UtcDateTime;
		var job = new Job(Guid.NewGuid(), ownerId, identity, connectionId, kind, displayName ?? kind, parameters.Clone(), payloadBytes, now);

		if (!jobs.TryAdd(job.Id, job) || !queue.Writer.TryWrite(job))
		{
			jobs.TryRemove(new KeyValuePair<Guid, Job>(job.Id, job));
			ReleaseParameterBudget(job);
			if (job.TryClaimCleanup())
			{
				Interlocked.Decrement(ref retainedJobCount);
				job.Cancellation.Dispose();
			}
			RejectedCounter.Add(1, new KeyValuePair<string, object?>("reason", "queue_full"));
			snapshot = null;
			return false;
		}

		StartedCounter.Add(1, new KeyValuePair<string, object?>("kind", kind));
		snapshot = job.Snapshot();
		return true;
	}

	public BackgroundJobSnapshot? Get(Guid jobId, Guid ownerId) =>
		jobs.TryGetValue(jobId, out var job) && job.OwnerId == ownerId ? job.Snapshot() : null;

	public IReadOnlyList<BackgroundJobSnapshot> ListForOwner(Guid ownerId) =>
		jobs.Values
			.Where(job => job.OwnerId == ownerId)
			.Select(job => job.Snapshot())
			.OrderByDescending(snapshot => snapshot.CreatedAt ?? snapshot.UpdatedAt)
			.ThenByDescending(snapshot => snapshot.UpdatedAt)
			.ToArray();

	public object? GetResult(Guid jobId, Guid ownerId) =>
		jobs.TryGetValue(jobId, out var job) && job.OwnerId == ownerId ? job.Result : null;

	public bool TryPinResultFile(Guid jobId, Guid ownerId, out BackgroundJobFile? file, out IDisposable? downloadLease)
	{
		if (jobs.TryGetValue(jobId, out var job) && job.OwnerId == ownerId)
		{
			return job.TryPinResultFile(out file, out downloadLease);
		}

		file = null;
		downloadLease = null;
		return false;
	}

	public async Task<bool> CancelAsync(Guid jobId, Guid ownerId)
	{
		if (!jobs.TryGetValue(jobId, out var job) || job.OwnerId != ownerId)
		{
			return false;
		}

		var changed = job.TryCancel(NowUtc());
		if (changed is not null)
		{
			await PublishOrderedAsync(job, changed);
		}
		return true;
	}

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		CleanupOrphanedTempFiles();
		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			var tasks = Enumerable.Range(0, options.WorkerCount)
				.Select(_ => RunWorkerAsync(stoppingToken))
				.Append(CleanupAsync(stoppingToken));
			await Task.WhenAll(tasks);
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await base.StopAsync(cancellationToken);
		foreach (var job in jobs.Values)
		{
			if (job.Result is BackgroundJobFile file)
			{
				DeleteFileBestEffort(file.Path, job.Id);
			}
		}
	}

	private DateTime NowUtc() => timeProvider.GetUtcNow().UtcDateTime;

	private bool TryReserveJobSlot()
	{
		while (true)
		{
			int current = Volatile.Read(ref retainedJobCount);
			if (current >= options.MaxRetainedJobs)
			{
				return false;
			}
			if (Interlocked.CompareExchange(ref retainedJobCount, current + 1, current) == current)
			{
				return true;
			}
		}
	}

	private bool TryReserveParameterBytes(long payloadBytes)
	{
		if (payloadBytes < 0 || payloadBytes > options.MaxActiveParameterBytes)
		{
			return false;
		}

		while (true)
		{
			long current = Volatile.Read(ref activeParameterBytes);
			if (payloadBytes > options.MaxActiveParameterBytes - current)
			{
				return false;
			}
			if (Interlocked.CompareExchange(ref activeParameterBytes, current + payloadBytes, current) == current)
			{
				return true;
			}
		}
	}

	private void ReleaseParameterBudget(Job job)
	{
		if (job.TryReleaseParameterBudget())
		{
			Interlocked.Add(ref activeParameterBytes, -job.ParameterBytes);
		}
	}

	private async Task CleanupAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(options.CleanupInterval, timeProvider);
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			RemoveExpired();
		}
	}

	/// <summary>Lock-free expiration pass with atomic cleanup ownership.</summary>
	internal void RemoveExpired()
	{
		var cutoff = NowUtc().Subtract(options.ResultRetention);
		foreach (var entry in jobs)
		{
			var current = entry.Value.Snapshot();
			if (!IsFinished(current.Status) || current.UpdatedAt >= cutoff)
			{
				continue;
			}

			if (entry.Value.HasActiveFileDownloads)
			{
				continue;
			}

			if (!jobs.TryRemove(new KeyValuePair<Guid, Job>(entry.Key, entry.Value)))
			{
				continue;
			}

			if (!entry.Value.TryClaimCleanup())
			{
				continue;
			}

			Interlocked.Decrement(ref retainedJobCount);
			subscriptionRegistry?.RemoveJob(entry.Key);

			if (entry.Value.Result is BackgroundJobFile file)
			{
				if (!entry.Value.HasActiveFileDownloads)
				{
					DeleteFileBestEffort(file.Path, entry.Key);
				}
				else
				{
					entry.Value.MarkPendingFileDelete(file.Path);
				}
			}

			entry.Value.Cancellation.Dispose();
		}
	}

	private void CleanupOrphanedTempFiles()
	{
		try
		{
			if (!Directory.Exists(BackgroundJobTempStorage.DirectoryPath))
			{
				return;
			}

			var cutoff = NowUtc().Subtract(options.ResultRetention);
			foreach (var filePath in Directory.EnumerateFiles(BackgroundJobTempStorage.DirectoryPath, "redisgui-job-*"))
			{
				try
				{
					var lastWrite = File.GetLastWriteTimeUtc(filePath);
					if (lastWrite < cutoff)
					{
						File.Delete(filePath);
					}
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
				{
					logger.LogDebug(ex, "Could not clean orphaned temp file {FilePath}", filePath);
				}
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			logger.LogDebug(ex, "Could not inspect background job temp directory");
		}
	}

	private void DeleteFileBestEffort(string path, Guid jobId)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			logger.LogWarning(ex, "Could not remove job file {JobId}", jobId);
		}
	}

	private async Task RunWorkerAsync(CancellationToken stoppingToken)
	{
		await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
		{
			using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.Cancellation.Token);
			try
			{
				var startSnapshot = job.TryStartRunning(NowUtc());
				if (startSnapshot is null)
				{
					// Job was already marked Cancelling while sitting in the queue.
					var cancelledFromQueue = job.FinishCancelled(
						value: null,
						error: stoppingToken.IsCancellationRequested && !job.Cancellation.IsCancellationRequested
							? "Job was cancelled because the server is shutting down."
							: null,
						now: NowUtc());
					if (cancelledFromQueue is not null)
					{
						CancelledCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
						await PublishOrderedAsync(job, cancelledFromQueue);
					}
					continue;
				}

				await PublishOrderedAsync(job, startSnapshot);

				using var scope = scopes.CreateScope();
				scope.ServiceProvider.GetRequiredService<JobIdentity>().Principal = job.Principal;
				var handler = scope.ServiceProvider.GetServices<IBackgroundJobHandler>().SingleOrDefault(x => x.Kind == job.Kind)
					?? throw new InvalidOperationException($"Job handler '{job.Kind}' is unavailable.");

				var result = await handler.ExecuteAsync(
					job.ConnectionId,
					job.Parameters,
					async (percent, metrics) =>
					{
						if (!linked.IsCancellationRequested)
						{
							var progressSnapshot = job.TryReportProgress(percent, metrics, NowUtc());
							if (progressSnapshot is not null)
							{
								await PublishOrderedAsync(job, progressSnapshot);
							}
						}
					},
					linked.Token);

				var terminal = ResolveExecutionOutcome(job, result, linked.IsCancellationRequested, stoppingToken.IsCancellationRequested);
				if (terminal is not null)
				{
					await PublishOrderedAsync(job, terminal);
				}
			}
			catch (OperationCanceledException) when (linked.IsCancellationRequested)
			{
				string? shutdownNote = stoppingToken.IsCancellationRequested && !job.Cancellation.IsCancellationRequested
					? "Job was cancelled because the server is shutting down."
					: null;
				var cancelledSnapshot = job.FinishCancelled(value: null, error: shutdownNote, now: NowUtc());
				if (cancelledSnapshot is not null)
				{
					CancelledCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
					await PublishOrderedAsync(job, cancelledSnapshot);
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Background job {JobId} failed", job.Id);
				var failedSnapshot = job.FinishFailed(
					"The operation failed. Check the server logs.",
					value: null,
					cancellationRequested: linked.IsCancellationRequested,
					mayHaveUnconfirmedChanges: true,
					now: NowUtc());
				if (failedSnapshot is not null)
				{
					FailedCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
					await PublishOrderedAsync(job, failedSnapshot);
				}
			}
			finally
			{
				ReleaseParameterBudget(job);
			}
		}
	}

	private BackgroundJobSnapshot? ResolveExecutionOutcome(
		Job job,
		Shared.Primitives.Result<object?> result,
		bool tokenCancelled,
		bool hostStopping)
	{
		var now = NowUtc();

		// Precedence rule 1: Operational failures and partial failures ALWAYS preserve Failed status
		// even when cancellation raced concurrently, so errors are never masked as Cancelled.
		if (result.IsFailure)
		{
			FailedCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
			return job.FinishFailed(
				result.Error.Message,
				value: null,
				cancellationRequested: tokenCancelled,
				mayHaveUnconfirmedChanges: false,
				now: now);
		}

		if (result.Value is BackgroundJobOutcome partialFailure)
		{
			FailedCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
			return job.FinishFailed(
				partialFailure.Error,
				value: partialFailure.Result,
				cancellationRequested: tokenCancelled || partialFailure.CancellationRequested,
				mayHaveUnconfirmedChanges: true,
				now: now);
		}

		// Precedence rule 2: Explicit cooperative cancellation or cancellation token fired before completion.
		if (result.Value is BackgroundJobCancellationOutcome cancelOutcome)
		{
			CancelledCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
			return job.FinishCancelled(cancelOutcome.Result, cancelOutcome.Message, now);
		}

		bool markedCancelled = IsResultMarkedCancelled(result.Value);
		if (tokenCancelled || markedCancelled || job.IsCancelling)
		{
			string? note = hostStopping && !job.Cancellation.IsCancellationRequested
				? "Job was cancelled because the server is shutting down."
				: null;
			CancelledCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
			return job.FinishCancelled(result.Value, note, now);
		}

		// Precedence rule 3: Clean completion.
		CompletedCounter.Add(1, new KeyValuePair<string, object?>("kind", job.Kind));
		return job.FinishCompleted(result.Value, now);
	}

	private static bool IsResultMarkedCancelled(object? value)
	{
		if (value is null)
		{
			return false;
		}

		var prop = value.GetType().GetProperty("Cancelled", BindingFlags.Instance | BindingFlags.Public);
		return prop?.PropertyType == typeof(bool) && prop.GetValue(value) is true;
	}

	/// <summary>
	/// Lock-free non-blocking MPSC event drainer ensuring per-job SignalR updates
	/// are always emitted in monotonic revision order.
	/// </summary>
	private async Task PublishOrderedAsync(Job job, BackgroundJobSnapshot snapshot)
	{
		job.PendingEvents.Enqueue(snapshot);
		if (Interlocked.CompareExchange(ref job.PublishDraining, 1, 0) != 0)
		{
			return;
		}

		while (true)
		{
			while (job.PendingEvents.TryDequeue(out var next))
			{
				if (next.Revision <= Volatile.Read(ref job.LastPublishedRevision))
				{
					continue;
				}

				Volatile.Write(ref job.LastPublishedRevision, next.Revision);
				await SendAuthorizedUpdateAsync(next);
			}

			Volatile.Write(ref job.PublishDraining, 0);
			if (job.PendingEvents.IsEmpty || Interlocked.CompareExchange(ref job.PublishDraining, 1, 0) != 0)
			{
				break;
			}
		}
	}

	private async Task SendAuthorizedUpdateAsync(BackgroundJobSnapshot snapshot)
	{
		try
		{
			var subscribers = subscriptionRegistry?.GetSubscribers(snapshot.JobId);
			if (subscribers is { Count: > 0 })
			{
				var authorizedIds = new List<string>(subscribers.Count);
				Domain.Connection.RedisConnection? connection = null;
				bool connectionLookupAttempted = false;

				using var scope = scopes.CreateScope();
				var repository = scope.ServiceProvider.GetService<Domain.Connection.IRedisConnectionRepository>();
				var authorization = scope.ServiceProvider.GetService<IAuthorizationService>();

				if (repository is not null && authorization is not null)
				{
					var connResult = await repository.GetConnectionByIdAsync(snapshot.ConnectionId);
					connectionLookupAttempted = true;
					if (connResult.IsSuccess)
					{
						connection = connResult.Value;
					}
				}

				foreach (var sub in subscribers)
				{
					bool stillAllowed = !connectionLookupAttempted ||
						(connection is not null &&
						 (await authorization!.AuthorizeAsync(sub.Principal, connection, new ConnectionOwnershipRequirement())).Succeeded);

					if (stillAllowed)
					{
						authorizedIds.Add(sub.ConnectionId);
					}
					else
					{
						subscriptionRegistry!.Unsubscribe(snapshot.JobId, sub.ConnectionId);
						await hub.Groups.RemoveFromGroupAsync(sub.ConnectionId, BackgroundJobsHub.Group(snapshot.JobId));
						await hub.Clients.Client(sub.ConnectionId).SendAsync("AccessRevoked", snapshot.JobId);
					}
				}

				if (authorizedIds.Count == 0)
				{
					return;
				}

				await hub.Clients.Clients(authorizedIds).SendAsync("JobUpdated", snapshot);
				if (IsFinished(snapshot.Status))
				{
					await hub.Clients.Clients(authorizedIds).SendAsync("JobCompleted", snapshot);
				}
				return;
			}

			var group = BackgroundJobsHub.Group(snapshot.JobId);
			await hub.Clients.Group(group).SendAsync("JobUpdated", snapshot);
			if (IsFinished(snapshot.Status))
			{
				await hub.Clients.Group(group).SendAsync("JobCompleted", snapshot);
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Could not publish job update {JobId}", snapshot.JobId);
		}
	}

	private static bool IsFinished(string status) => status is "Completed" or "Failed" or "Cancelled";

	private sealed record JobState(
		string Status,
		long Revision,
		double? ProgressPercent,
		object? Metrics,
		string? Error,
		object? Result,
		DateTime UpdatedAt,
		DateTime CreatedAt,
		DateTime? StartedAt,
		DateTime? CompletedAt,
		bool CancellationRequested,
		bool MayHaveUnconfirmedChanges);

	private sealed class Job(
		Guid id,
		Guid ownerId,
		ClaimsPrincipal principal,
		Guid connectionId,
		string kind,
		string displayName,
		JsonElement parameters,
		long parameterBytes,
		DateTime createdAt)
	{
		private JobState state = new(
			"Queued",
			Revision: 0,
			ProgressPercent: null,
			Metrics: null,
			Error: null,
			Result: null,
			UpdatedAt: createdAt,
			CreatedAt: createdAt,
			StartedAt: null,
			CompletedAt: null,
			CancellationRequested: false,
			MayHaveUnconfirmedChanges: false);

		private JsonElement parameters = parameters;
		private int parameterBudgetReleased;
		private int cleanupClaimed;
		private int activeFileDownloads;
		private string? pendingDeletePath;

		public Guid Id { get; } = id;
		public Guid OwnerId { get; } = ownerId;
		public ClaimsPrincipal Principal { get; } = principal;
		public Guid ConnectionId { get; } = connectionId;
		public string Kind { get; } = kind;
		public string DisplayName { get; } = displayName;
		public long ParameterBytes { get; } = parameterBytes;
		public CancellationTokenSource Cancellation { get; } = new();
		public ConcurrentQueue<BackgroundJobSnapshot> PendingEvents { get; } = new();
		public int PublishDraining;
		public long LastPublishedRevision = -1;

		public JsonElement Parameters => parameters;
		public bool IsCancelling => Volatile.Read(ref state).Status == "Cancelling";
		public bool HasActiveFileDownloads => Volatile.Read(ref activeFileDownloads) > 0;

		public object? Result
		{
			get
			{
				var current = Volatile.Read(ref state);
				return IsFinished(current.Status) ? current.Result : null;
			}
		}

		public BackgroundJobSnapshot Snapshot() => ToSnapshot(Volatile.Read(ref state));

		public bool TryReleaseParameterBudget()
		{
			if (Interlocked.CompareExchange(ref parameterBudgetReleased, 1, 0) != 0)
			{
				return false;
			}

			parameters = default;
			return true;
		}

		public bool TryClaimCleanup() => Interlocked.CompareExchange(ref cleanupClaimed, 1, 0) == 0;

		public void MarkPendingFileDelete(string path) => Volatile.Write(ref pendingDeletePath, path);

		public bool TryPinResultFile(out BackgroundJobFile? file, out IDisposable? lease)
		{
			var current = Volatile.Read(ref state);
			if (!IsFinished(current.Status) || current.Result is not BackgroundJobFile jobFile)
			{
				file = null;
				lease = null;
				return false;
			}

			Interlocked.Increment(ref activeFileDownloads);
			if (Volatile.Read(ref cleanupClaimed) != 0)
			{
				ReleaseDownloadLease();
				file = null;
				lease = null;
				return false;
			}

			file = jobFile;
			lease = new DownloadLease(this);
			return true;
		}

		private void ReleaseDownloadLease()
		{
			if (Interlocked.Decrement(ref activeFileDownloads) == 0)
			{
				var path = Interlocked.Exchange(ref pendingDeletePath, null);
				if (path is not null)
				{
					try
					{
						if (File.Exists(path)) File.Delete(path);
					}
					catch
					{
						// Best-effort cleanup of expired download file.
					}
				}
			}
		}

		public BackgroundJobSnapshot? TryStartRunning(DateTime now) =>
			CasUpdate(current => current.Status == "Queued"
				? current with { Status = "Running", Revision = current.Revision + 1, StartedAt = now, UpdatedAt = now }
				: null);

		public BackgroundJobSnapshot? TryReportProgress(double? percent, object? value, DateTime now)
		{
			double? normalized = percent.HasValue && double.IsFinite(percent.Value)
				? Math.Clamp(percent.Value, 0, 100)
				: null;

			return CasUpdate(current => current.Status == "Running"
				? current with
				{
					ProgressPercent = normalized,
					Metrics = value,
					Revision = current.Revision + 1,
					UpdatedAt = now
				}
				: null);
		}

		public BackgroundJobSnapshot? TryCancel(DateTime now)
		{
			var changed = CasUpdate(current =>
				IsFinished(current.Status) || current.Status == "Cancelling"
					? null
					: current with
					{
						Status = "Cancelling",
						CancellationRequested = true,
						Revision = current.Revision + 1,
						UpdatedAt = now
					});

			if (changed is not null)
			{
				try
				{
					Cancellation.Cancel();
				}
				catch (ObjectDisposedException)
				{
				}
			}

			return changed;
		}

		public BackgroundJobSnapshot? FinishCompleted(object? value, DateTime now) =>
			CasUpdate(current => IsFinished(current.Status)
				? null
				: current with
				{
					Status = "Completed",
					ProgressPercent = 100,
					Result = value,
					Revision = current.Revision + 1,
					CompletedAt = now,
					UpdatedAt = now
				});

		public BackgroundJobSnapshot? FinishCancelled(object? value, string? error, DateTime now) =>
			CasUpdate(current => IsFinished(current.Status)
				? null
				: current with
				{
					Status = "Cancelled",
					Error = error,
					Result = value,
					CancellationRequested = true,
					Revision = current.Revision + 1,
					CompletedAt = now,
					UpdatedAt = now
				});

		public BackgroundJobSnapshot? FinishFailed(
			string failure,
			object? value,
			bool cancellationRequested,
			bool mayHaveUnconfirmedChanges,
			DateTime now) =>
			CasUpdate(current => IsFinished(current.Status)
				? null
				: current with
				{
					Status = "Failed",
					Error = failure,
					Result = value,
					CancellationRequested = current.CancellationRequested || cancellationRequested,
					MayHaveUnconfirmedChanges = mayHaveUnconfirmedChanges,
					Revision = current.Revision + 1,
					CompletedAt = now,
					UpdatedAt = now
				});

		private BackgroundJobSnapshot? CasUpdate(Func<JobState, JobState?> transform)
		{
			while (true)
			{
				var current = Volatile.Read(ref state);
				var next = transform(current);
				if (next is null)
				{
					return null;
				}
				if (ReferenceEquals(Interlocked.CompareExchange(ref state, next, current), current))
				{
					return ToSnapshot(next);
				}
			}
		}

		private BackgroundJobSnapshot ToSnapshot(JobState current) =>
			new(
				Id,
				ConnectionId,
				Kind,
				current.Status,
				current.Revision,
				current.ProgressPercent,
				current.Metrics,
				current.Error,
				IsFinished(current.Status) && current.Result is not null,
				current.UpdatedAt,
				current.CreatedAt,
				current.StartedAt,
				current.CompletedAt,
				current.CancellationRequested,
				current.MayHaveUnconfirmedChanges,
				DisplayName);

		private sealed class DownloadLease(Job owner) : IDisposable
		{
			private int disposed;

			public void Dispose()
			{
				if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
				{
					owner.ReleaseDownloadLease();
				}
			}
		}
	}
}
