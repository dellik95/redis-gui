import { computed, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { Observable, Subscription, catchError, of, tap } from 'rxjs';
import {
  BackgroundJob,
  BackgroundJobKindInfo,
  TrackedJobEntry,
  describeExpiredJobReason,
  describeJobDisplayName,
  isJobActive,
  isJobTerminal
} from '../models/background-jobs.types';
import { BackgroundJobsService } from '../services/background-jobs.service';

interface PersistedJobHint {
  jobId: string;
  connectionId: string;
  kind: string;
  displayName?: string | null | undefined;
  format?: 'json' | 'csv' | undefined;
  database?: number | undefined;
  seenRevision: number;
  dismissed: boolean;
  updatedAt: string;
}

export interface TrackJobOptions {
  format?: 'json' | 'csv' | undefined;
  database?: number | undefined;
  markSeen?: boolean | undefined;
}

interface BackgroundJobsState {
  userScope: string | null;
  jobsById: Record<string, TrackedJobEntry>;
  jobOrder: string[];
  kinds: BackgroundJobKindInfo[];
  liveConnected: boolean;
  connectionNotice: string | null;
  isLoading: boolean;
}

const initialState: BackgroundJobsState = {
  userScope: null,
  jobsById: {},
  jobOrder: [],
  kinds: [],
  liveConnected: true,
  connectionNotice: null,
  isLoading: false
};

const MAX_RETAINED_CLIENT_JOBS = 40;
const STORAGE_PREFIX = 'redisgui:jobs:v1:';

export const BackgroundJobsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ jobsById, jobOrder }) => {
    const visibleJobs = computed<TrackedJobEntry[]>(() => {
      const map = jobsById();
      const entries: TrackedJobEntry[] = [];
      for (const id of jobOrder()) {
        const entry = map[id];
        if (entry && !entry.dismissed) {
          entries.push(entry);
        }
      }
      return entries;
    });

    const activeJobs = computed<TrackedJobEntry[]>(() =>
      visibleJobs().filter(entry => isJobActive(entry.job.status))
    );

    const activeCount = computed(() => activeJobs().length);

    const unreadCount = computed(() =>
      visibleJobs().filter(
        entry =>
          (isJobTerminal(entry.job.status) || entry.expiredReason !== null) &&
          entry.job.revision > entry.seenRevision
      ).length
    );

    const badgeCount = computed(() => activeCount() + unreadCount());

    return {
      visibleJobs,
      activeJobs,
      activeCount,
      unreadCount,
      badgeCount
    };
  }),
  withMethods(store => {
    const jobsService = inject(BackgroundJobsService);
    const snackBar = inject(MatSnackBar);

    const watchers = new Map<string, Subscription>();
    const pendingResults = new Set<string>();

    const storageKeyFor = (userScope: string) => `${STORAGE_PREFIX}${userScope}`;

    const persistHints = () => {
      const scope = store.userScope();
      if (!scope) return;

      try {
        const hints: PersistedJobHint[] = [];
        const map = store.jobsById();
        for (const id of store.jobOrder().slice(0, MAX_RETAINED_CLIENT_JOBS)) {
          const entry = map[id];
          if (!entry) continue;
          hints.push({
            jobId: entry.job.jobId,
            connectionId: entry.job.connectionId,
            kind: entry.job.kind,
            displayName: entry.job.displayName ?? null,
            format: entry.format,
            database: entry.database,
            seenRevision: entry.seenRevision,
            dismissed: entry.dismissed,
            updatedAt: entry.job.updatedAt
          });
        }
        sessionStorage.setItem(storageKeyFor(scope), JSON.stringify(hints));
      } catch {
        /* Ignore storage quota issues. */
      }
    };

    const readHints = (scope: string): PersistedJobHint[] => {
      try {
        const raw = sessionStorage.getItem(storageKeyFor(scope));
        if (!raw) return [];
        const parsed: unknown = JSON.parse(raw);
        if (!Array.isArray(parsed)) return [];
        return parsed.filter(
          (item): item is PersistedJobHint =>
            typeof item === 'object' &&
            item !== null &&
            typeof (item as PersistedJobHint).jobId === 'string' &&
            typeof (item as PersistedJobHint).kind === 'string'
        );
      } catch {
        return [];
      }
    };

    const stopWatcher = (jobId: string) => {
      const sub = watchers.get(jobId);
      if (sub) {
        sub.unsubscribe();
        watchers.delete(jobId);
      }
    };

    const stopAllWatchers = () => {
      for (const sub of watchers.values()) {
        sub.unsubscribe();
      }
      watchers.clear();
      pendingResults.clear();
    };

    const fetchResultIfNeeded = (job: BackgroundJob) => {
      if (!job.hasResult || job.kind === 'redis-export') return;
      const existing = store.jobsById()[job.jobId];
      if (!existing || existing.resultLoaded || pendingResults.has(job.jobId)) return;

      pendingResults.add(job.jobId);
      jobsService.result<unknown>(job.jobId).subscribe({
        next: data => {
          pendingResults.delete(job.jobId);
          const current = store.jobsById()[job.jobId];
          if (!current) return;
          patchState(store, {
            jobsById: {
              ...store.jobsById(),
              [job.jobId]: {
                ...current,
                resultLoaded: true,
                resultError: null,
                resultData: data
              }
            }
          });
        },
        error: () => {
          pendingResults.delete(job.jobId);
          const current = store.jobsById()[job.jobId];
          if (!current) return;
          patchState(store, {
            jobsById: {
              ...store.jobsById(),
              [job.jobId]: {
                ...current,
                resultError: 'Could not retrieve the job result. Refresh status to retry.'
              }
            }
          });
        }
      });
    };

    const notifyTerminal = (job: BackgroundJob) => {
      const label = describeJobDisplayName(job);
      if (job.status === 'Completed') {
        snackBar.open(`${label} completed.`, 'View', { duration: 4000 });
      } else if (job.status === 'Failed') {
        snackBar.open(job.error || `${label} failed.`, 'Dismiss', { duration: 6000 });
      } else if (job.status === 'Cancelled') {
        snackBar.open(`${label} stopped.`, 'Dismiss', { duration: 4000 });
      }
    };

    const ensureWatcher = (jobId: string) => {
      if (watchers.has(jobId)) return;

      const sub = jobsService.watch(jobId).subscribe(event => {
        if (event.revoked) {
          stopWatcher(jobId);
          const current = store.jobsById()[jobId];
          if (current) {
            patchState(store, {
              jobsById: {
                ...store.jobsById(),
                [jobId]: {
                  ...current,
                  job: {
                    ...current.job,
                    status: 'Failed',
                    error: event.error ?? 'Access to this background job was revoked.'
                  },
                  expiredReason: event.error ?? 'Access to this background job was revoked.'
                }
              }
            });
            persistHints();
          }
          snackBar.open(event.error ?? 'Access to background job was revoked.', 'Dismiss', {
            duration: 5000
          });
          return;
        }

        patchState(store, {
          liveConnected: event.connected,
          connectionNotice: event.connected ? null : (event.error ?? 'Reconnecting live job updates…')
        });

        if (event.job) {
          applySnapshot(event.job);
        }
      });

      watchers.set(jobId, sub);
    };

    const applySnapshot = (
      job: BackgroundJob,
      options?: TrackJobOptions & { notifyOnTransition?: boolean | undefined }
    ) => {
      const currentMap = store.jobsById();
      const existing = currentMap[job.jobId];
      if (existing && job.revision < existing.job.revision) {
        return;
      }

      const wasActive = existing ? isJobActive(existing.job.status) : true;
      const nowTerminal = isJobTerminal(job.status);
      const seenRevision = options?.markSeen
        ? job.revision
        : existing
          ? existing.seenRevision
          : -1;

      const nextEntry: TrackedJobEntry = {
        job,
        seenRevision,
        dismissed: options?.markSeen ? false : (existing?.dismissed ?? false),
        expiredReason: null,
        resultLoaded: existing?.resultLoaded ?? false,
        resultError: existing?.resultError ?? null,
        resultData: existing?.resultData ?? null,
        format: options?.format ?? existing?.format,
        database: options?.database ?? existing?.database
      };

      const nextOrder = [
        job.jobId,
        ...store.jobOrder().filter(id => id !== job.jobId)
      ].slice(0, MAX_RETAINED_CLIENT_JOBS);

      const nextMap: Record<string, TrackedJobEntry> = {};
      for (const id of nextOrder) {
        if (id === job.jobId) {
          nextMap[id] = nextEntry;
        } else if (currentMap[id]) {
          nextMap[id] = currentMap[id];
        }
      }

      patchState(store, {
        jobsById: nextMap,
        jobOrder: nextOrder
      });
      persistHints();

      if (isJobActive(job.status)) {
        ensureWatcher(job.jobId);
      } else {
        stopWatcher(job.jobId);
        fetchResultIfNeeded(job);
        if (nowTerminal && wasActive && existing && (options?.notifyOnTransition ?? true)) {
          notifyTerminal(job);
        }
      }
    };

    const markJobExpired = (jobId: string, reason = describeExpiredJobReason()) => {
      stopWatcher(jobId);
      const existing = store.jobsById()[jobId];
      if (!existing) return;

      patchState(store, {
        jobsById: {
          ...store.jobsById(),
          [jobId]: {
            ...existing,
            job: {
              ...existing.job,
              status: 'Failed',
              error: reason,
              revision: existing.job.revision + 1
            },
            expiredReason: reason
          }
        }
      });
      persistHints();
    };

    return {
      syncForUser(userId: string): void {
        const previousScope = store.userScope();
        if (previousScope !== userId) {
          stopAllWatchers();
          jobsService.disconnect();
          patchState(store, {
            ...initialState,
            userScope: userId,
            isLoading: true
          });
        } else {
          patchState(store, { isLoading: true });
        }

        const hints = readHints(userId);
        const hintMap = new Map(hints.map(h => [h.jobId, h]));

        jobsService.kinds().pipe(catchError(() => of([] as BackgroundJobKindInfo[]))).subscribe(kinds => {
          patchState(store, { kinds });
        });

        jobsService.list().subscribe({
          next: serverJobs => {
            const nextMap: Record<string, TrackedJobEntry> = {};
            const order: string[] = [];

            for (const job of serverJobs) {
              const hint = hintMap.get(job.jobId);
              nextMap[job.jobId] = {
                job,
                seenRevision: hint?.seenRevision ?? (isJobTerminal(job.status) ? job.revision : -1),
                dismissed: hint?.dismissed ?? false,
                expiredReason: null,
                resultLoaded: false,
                resultError: null,
                resultData: null,
                format: hint?.format,
                database: hint?.database
              };
              order.push(job.jobId);
            }

            // Reconcile persisted hints no longer in active/retained server list
            for (const hint of hints) {
              if (nextMap[hint.jobId] || hint.dismissed) continue;
              nextMap[hint.jobId] = {
                job: {
                  jobId: hint.jobId,
                  connectionId: hint.connectionId,
                  kind: hint.kind,
                  displayName: hint.displayName ?? describeJobDisplayName({ kind: hint.kind, displayName: null }),
                  status: 'Failed',
                  revision: hint.seenRevision + 1,
                  progressPercent: null,
                  metrics: null,
                  error: describeExpiredJobReason(),
                  hasResult: false,
                  updatedAt: hint.updatedAt
                },
                seenRevision: hint.seenRevision,
                dismissed: false,
                expiredReason: describeExpiredJobReason(),
                resultLoaded: false,
                resultError: null,
                resultData: null,
                format: hint.format,
                database: hint.database
              };
              order.push(hint.jobId);
            }

            patchState(store, {
              jobsById: nextMap,
              jobOrder: order.slice(0, MAX_RETAINED_CLIENT_JOBS),
              isLoading: false
            });
            persistHints();

            for (const job of serverJobs) {
              if (isJobActive(job.status)) {
                ensureWatcher(job.jobId);
              } else if (job.hasResult) {
                fetchResultIfNeeded(job);
              }
            }
          },
          error: () => {
            patchState(store, { isLoading: false });
          }
        });
      },

      startJob<TParams = unknown>(
        kind: string,
        connectionId: string,
        parameters: TParams,
        options?: TrackJobOptions
      ): Observable<BackgroundJob> {
        return jobsService.start(kind, connectionId, parameters).pipe(
          tap(job => applySnapshot(job, { ...options, notifyOnTransition: true }))
        );
      },

      trackJob(job: BackgroundJob, options?: TrackJobOptions): void {
        applySnapshot(job, { ...options, notifyOnTransition: true });
      },

      refreshJob(jobId: string): void {
        jobsService.status(jobId).subscribe({
          next: job => applySnapshot(job, { notifyOnTransition: false }),
          error: (err: unknown) => {
            if (err instanceof HttpErrorResponse && err.status === 404) {
              markJobExpired(jobId);
            }
          }
        });
      },

      cancelJob(jobId: string): Observable<BackgroundJob | null> {
        return jobsService.cancel(jobId).pipe(
          tap(job => applySnapshot(job, { notifyOnTransition: true })),
          catchError((err: unknown) => {
            if (err instanceof HttpErrorResponse && err.status === 404) {
              markJobExpired(jobId);
            }
            return of(null);
          })
        );
      },

      loadResult(jobId: string): void {
        const entry = store.jobsById()[jobId];
        if (entry) {
          fetchResultIfNeeded(entry.job);
        }
      },

      markJobExpired(jobId: string, reason?: string): void {
        markJobExpired(jobId, reason);
      },

      markAllRead(): void {
        const current = store.jobsById();
        let changed = false;
        const next: Record<string, TrackedJobEntry> = { ...current };
        for (const id of Object.keys(current)) {
          const entry = current[id];
          if (entry && !entry.dismissed && entry.seenRevision < entry.job.revision) {
            next[id] = { ...entry, seenRevision: entry.job.revision };
            changed = true;
          }
        }
        if (changed) {
          patchState(store, { jobsById: next });
          persistHints();
        }
      },

      dismissJob(jobId: string): void {
        stopWatcher(jobId);
        const existing = store.jobsById()[jobId];
        if (!existing) return;

        patchState(store, {
          jobsById: {
            ...store.jobsById(),
            [jobId]: {
              ...existing,
              dismissed: true,
              seenRevision: existing.job.revision
            }
          }
        });
        persistHints();
      },

      clearCompleted(): void {
        const current = store.jobsById();
        const next: Record<string, TrackedJobEntry> = {};
        const nextOrder: string[] = [];

        for (const id of store.jobOrder()) {
          const entry = current[id];
          if (!entry) continue;
          if (isJobActive(entry.job.status)) {
            next[id] = entry;
            nextOrder.push(id);
          } else {
            next[id] = { ...entry, dismissed: true, seenRevision: entry.job.revision };
            nextOrder.push(id);
          }
        }

        patchState(store, { jobsById: next, jobOrder: nextOrder });
        persistHints();
      },

      entrySignal(jobId: string) {
        return computed(() => store.jobsById()[jobId] ?? null);
      },

      findExportFormat(jobId: string): 'json' | 'csv' {
        return store.jobsById()[jobId]?.format ?? 'json';
      },

      resetSession(clearPersisted = false): void {
        const previousScope = store.userScope();
        stopAllWatchers();
        jobsService.disconnect();
        if (clearPersisted && previousScope) {
          try {
            sessionStorage.removeItem(storageKeyFor(previousScope));
          } catch {
            /* Ignore */
          }
        }
        patchState(store, initialState);
      }
    };
  })
);
