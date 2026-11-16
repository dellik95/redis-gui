import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, firstValueFrom } from 'rxjs';
import { TokenStorageService } from './auth/token-storage.service';

export interface BackgroundJob {
  jobId: string;
  connectionId: string;
  kind: string;
  status: 'Queued' | 'Running' | 'Cancelling' | 'Cancelled' | 'Completed' | 'Failed';
  revision: number;
  progressPercent: number | null;
  metrics: unknown;
  error: string | null;
  hasResult: boolean;
  updatedAt: string;
}

export interface JobEvent { job?: BackgroundJob; connected: boolean; error?: string; }

@Injectable({ providedIn: 'root' })
export class JobHubFactory {
  private readonly tokens = inject(TokenStorageService);
  create(): HubConnection {
    return new HubConnectionBuilder().withUrl('/hub/jobs', { accessTokenFactory: () => this.tokens.token() ?? '' })
      .withAutomaticReconnect([0, 1000, 5000, 10000, 30000]).configureLogging(LogLevel.Warning).build();
  }
}

@Injectable({ providedIn: 'root' })
export class BackgroundJobsService {
  private readonly http = inject(HttpClient);
  private readonly hubs = inject(JobHubFactory);

  start(kind: string, connectionId: string, parameters: unknown): Observable<BackgroundJob> {
    return this.http.post<BackgroundJob>('/api/v1/jobs', { kind, connectionId, parameters });
  }

  status(jobId: string): Observable<BackgroundJob> { return this.http.get<BackgroundJob>(`/api/v1/jobs/${jobId}`); }
  result<T>(jobId: string): Observable<T> { return this.http.get<T>(`/api/v1/jobs/${jobId}/result`); }
  cancel(jobId: string): Observable<BackgroundJob> { return this.http.post<BackgroundJob>(`/api/v1/jobs/${jobId}/cancel`, {}); }

  watch(jobId: string): Observable<JobEvent> {
    return new Observable<JobEvent>(subscriber => {
      const hub = this.hubs.create();
      let connected = false;
      let latestRevision = -1;
      let closed = false;
      const emit = (job: BackgroundJob) => {
        if (!closed && job.jobId === jobId && job.revision > latestRevision) {
          latestRevision = job.revision;
          subscriber.next({ job, connected });
        }
      };
      const recover = async () => {
        const job = await firstValueFrom(this.status(jobId));
        emit(job);
      };
      const subscribeAndRecover = async () => {
        await hub.invoke('Subscribe', jobId);
        if (closed) return;
        connected = true;
        subscriber.next({ connected: true });
        await recover();
      };
      const reportFailure = async () => {
        if (closed) return;
        connected = false;
        subscriber.next({ connected: false, error: 'Live updates are unavailable. Refresh status to check the job.' });
        try { await recover(); } catch { /* The user can retry status explicitly. */ }
      };
      hub.on('JobUpdated', emit);
      hub.on('JobCompleted', emit);
      hub.onreconnecting(() => { connected = false; if (!closed) subscriber.next({ connected: false }); });
      hub.onreconnected(() => { void subscribeAndRecover().catch(reportFailure); });
      hub.onclose(() => { void reportFailure(); });
      void hub.start().then(subscribeAndRecover).catch(reportFailure);
      return () => { closed = true; void hub.stop(); };
    });
  }
}
