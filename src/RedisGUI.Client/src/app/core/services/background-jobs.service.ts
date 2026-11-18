import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, firstValueFrom } from 'rxjs';
import { API_ROUTES, SIGNALR_HUBS } from '../constants/api-routes.constants';
import { TokenStorageService } from './auth/token-storage.service';
import {
  BackgroundJob,
  BackgroundJobKindInfo,
  JobEvent
} from '../models/background-jobs.types';

export type {
  BackgroundJob,
  BackgroundJobKind,
  BackgroundJobKindInfo,
  BackgroundJobStatus,
  BulkKeyJobParameters,
  BulkKeyJobProgress,
  BulkKeyJobResult,
  ExportJobParameters,
  ExportJobProgress,
  ImportJobParameters,
  ImportJobProgress,
  JobEvent
} from '../models/background-jobs.types';

const INITIAL_START_BACKOFF_MS = [500, 1500, 3000] as const;

interface WatcherEntry {
  readonly jobId: string;
  lastRevision: number;
  readonly next: (event: JobEvent) => void;
}

@Injectable({ providedIn: 'root' })
export class JobHubFactory {
  private readonly tokens = inject(TokenStorageService);

  create(): HubConnection {
    return new HubConnectionBuilder()
      .withUrl(SIGNALR_HUBS.JOBS, { accessTokenFactory: () => this.tokens.token() ?? '' })
      .withAutomaticReconnect([0, 1000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();
  }
}

@Injectable({ providedIn: 'root' })
export class BackgroundJobsService {
  private readonly http = inject(HttpClient);
  private readonly hubs = inject(JobHubFactory);

  private sharedHub: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private connected = false;
  private hubGeneration = 0;
  private readonly watchersByJob = new Map<string, Set<WatcherEntry>>();

  kinds(): Observable<BackgroundJobKindInfo[]> {
    return this.http.get<BackgroundJobKindInfo[]>(API_ROUTES.JOBS.KINDS);
  }

  list(connectionId?: string, status?: string): Observable<BackgroundJob[]> {
    return this.http.get<BackgroundJob[]>(API_ROUTES.JOBS.LIST(connectionId, status));
  }

  start<TParams = unknown>(
    kind: string,
    connectionId: string,
    parameters: TParams
  ): Observable<BackgroundJob> {
    return this.http.post<BackgroundJob>(API_ROUTES.JOBS.BASE, { kind, connectionId, parameters });
  }

  status(jobId: string): Observable<BackgroundJob> {
    return this.http.get<BackgroundJob>(API_ROUTES.JOBS.GET(jobId));
  }

  result<T>(jobId: string): Observable<T> {
    return this.http.get<T>(API_ROUTES.JOBS.RESULT(jobId));
  }

  cancel(jobId: string): Observable<BackgroundJob> {
    return this.http.post<BackgroundJob>(API_ROUTES.JOBS.CANCEL(jobId), {});
  }

  disconnect(): void {
    this.hubGeneration++;
    const hub = this.sharedHub;
    this.sharedHub = null;
    this.startPromise = null;
    this.connected = false;
    this.watchersByJob.clear();
    if (hub) {
      void hub.stop().catch(() => undefined);
    }
  }

  watch(jobId: string): Observable<JobEvent> {
    return new Observable<JobEvent>(subscriber => {
      const entry: WatcherEntry = {
        jobId,
        lastRevision: -1,
        next: event => subscriber.next(event)
      };

      let jobWatchers = this.watchersByJob.get(jobId);
      const isFirstForJob = !jobWatchers || jobWatchers.size === 0;
      if (!jobWatchers) {
        jobWatchers = new Set<WatcherEntry>();
        this.watchersByJob.set(jobId, jobWatchers);
      }
      jobWatchers.add(entry);

      let unsubscribed = false;
      const hub = this.ensureSharedHub();
      const generation = this.hubGeneration;

      const subscribeAndRecover = async () => {
        await this.subscribeAndRecoverEntry(hub, entry, () => unsubscribed || generation !== this.hubGeneration);
      };

      void (this.connected ? subscribeAndRecover() : this.startSharedHubWithRetry(hub, generation).then(() => {
        if (!unsubscribed && generation === this.hubGeneration) {
          return subscribeAndRecover();
        }
        return Promise.resolve();
      })).catch(() => {
        if (!unsubscribed && generation === this.hubGeneration) {
          void this.reportJobFallback(entry);
        }
      });

      return () => {
        unsubscribed = true;
        const set = this.watchersByJob.get(jobId);
        if (set) {
          set.delete(entry);
          if (set.size === 0) {
            this.watchersByJob.delete(jobId);
            if (isFirstForJob && this.connected && this.sharedHub === hub) {
              void hub.invoke('Unsubscribe', jobId).catch(() => undefined);
            }
          }
        }

        if (this.watchersByJob.size === 0 && this.sharedHub === hub) {
          this.stopSharedHub(hub);
        }
      };
    });
  }

  private ensureSharedHub(): HubConnection {
    if (this.sharedHub) {
      return this.sharedHub;
    }

    const generation = ++this.hubGeneration;
    const hub = this.hubs.create();
    this.sharedHub = hub;

    const handleSnapshot = (job: BackgroundJob) => {
      if (generation !== this.hubGeneration || !job) return;
      const watchers = this.watchersByJob.get(job.jobId);
      if (!watchers) return;
      for (const watcher of watchers) {
        this.emitJobIfNewer(watcher, job);
      }
    };

    hub.on('JobUpdated', handleSnapshot);
    hub.on('JobCompleted', handleSnapshot);
    hub.on('AccessRevoked', (revokedJobId: string) => {
      if (generation !== this.hubGeneration) return;
      const watchers = this.watchersByJob.get(revokedJobId);
      if (!watchers) return;
      for (const watcher of watchers) {
        watcher.next({
          connected: false,
          revoked: true,
          error: 'Access to this background job was revoked.'
        });
      }
    });

    hub.onreconnecting(() => {
      if (generation !== this.hubGeneration) return;
      this.connected = false;
      this.broadcastConnectionState(false);
    });

    hub.onreconnected(() => {
      if (generation !== this.hubGeneration) return;
      void this.resubscribeAll(hub, generation);
    });

    hub.onclose(() => {
      if (generation !== this.hubGeneration) return;
      this.connected = false;
      this.startPromise = null;
      this.sharedHub = null;
      this.broadcastConnectionState(false, 'Live updates are unavailable. Refresh status to check the job.');
      void this.recoverAllWatchers(generation);
    });

    return hub;
  }

  private async startSharedHubWithRetry(hub: HubConnection, generation: number): Promise<void> {
    if (this.startPromise) {
      return this.startPromise;
    }

    const run = (async () => {
      let lastError: unknown;
      for (let attempt = 0; attempt <= INITIAL_START_BACKOFF_MS.length; attempt++) {
        if (generation !== this.hubGeneration || this.watchersByJob.size === 0) {
          return;
        }

        try {
          await hub.start();
          if (generation !== this.hubGeneration || this.watchersByJob.size === 0) {
            return;
          }
          this.connected = true;
          return;
        } catch (err) {
          lastError = err;
          if (attempt < INITIAL_START_BACKOFF_MS.length) {
            await delay(INITIAL_START_BACKOFF_MS[attempt] ?? 1000);
          }
        }
      }

      this.connected = false;
      throw lastError ?? new Error('Unable to connect to background job hub.');
    })();

    this.startPromise = run;
    try {
      await run;
    } finally {
      if (this.startPromise === run) {
        this.startPromise = null;
      }
    }
  }

  private async subscribeAndRecoverEntry(
    hub: HubConnection,
    entry: WatcherEntry,
    isCancelled: () => boolean
  ): Promise<void> {
    await hub.invoke('Subscribe', entry.jobId);
    if (isCancelled()) return;
    this.connected = true;
    entry.next({ connected: true });
    await this.recoverEntry(entry, isCancelled);
  }

  private async recoverEntry(entry: WatcherEntry, isCancelled: () => boolean): Promise<void> {
    const job = await firstValueFrom(this.status(entry.jobId));
    if (!isCancelled()) {
      this.emitJobIfNewer(entry, job);
    }
  }

  private async resubscribeAll(hub: HubConnection, generation: number): Promise<void> {
    const entries = this.snapshotAllWatchers();
    for (const entry of entries) {
      if (generation !== this.hubGeneration) return;
      try {
        await this.subscribeAndRecoverEntry(hub, entry, () => generation !== this.hubGeneration);
      } catch {
        await this.reportJobFallback(entry);
      }
    }
  }

  private async recoverAllWatchers(generation: number): Promise<void> {
    for (const entry of this.snapshotAllWatchers()) {
      if (generation !== this.hubGeneration) return;
      try {
        await this.recoverEntry(entry, () => generation !== this.hubGeneration);
      } catch {
        /* Explicit status refresh remains available. */
      }
    }
  }

  private async reportJobFallback(entry: WatcherEntry): Promise<void> {
    this.connected = false;
    entry.next({
      connected: false,
      error: 'Live updates are unavailable. Refresh status to check the job.'
    });
    try {
      await this.recoverEntry(entry, () => false);
    } catch {
      /* User can refresh status explicitly. */
    }
  }

  private emitJobIfNewer(entry: WatcherEntry, job: BackgroundJob): void {
    if (job.jobId === entry.jobId && job.revision > entry.lastRevision) {
      entry.lastRevision = job.revision;
      entry.next({ job, connected: this.connected });
    }
  }

  private broadcastConnectionState(connected: boolean, error?: string): void {
    for (const entry of this.snapshotAllWatchers()) {
      entry.next(error ? { connected, error } : { connected });
    }
  }

  private snapshotAllWatchers(): WatcherEntry[] {
    const all: WatcherEntry[] = [];
    for (const set of this.watchersByJob.values()) {
      for (const entry of set) {
        all.push(entry);
      }
    }
    return all;
  }

  private stopSharedHub(hub: HubConnection): void {
    this.hubGeneration++;
    this.sharedHub = null;
    this.startPromise = null;
    this.connected = false;
    void hub.stop().catch(() => undefined);
  }
}

function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}
