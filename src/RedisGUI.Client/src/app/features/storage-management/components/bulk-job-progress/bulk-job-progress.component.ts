import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';
import { BackgroundJob, BackgroundJobsService } from '../../../../core/services/background-jobs.service';

export interface BulkJobDialogData {
  connectionId: string;
  database: number;
  operation: 'delete-pattern' | 'delete' | 'ttl';
  pattern?: string;
  keys?: string[];
  ttlSeconds?: number | null;
}

interface BulkJobResult {
  processed: number;
  total: number | null;
  changed: number;
  unchanged: number;
  failed: number;
  failedKeys: string[];
  cancelled: boolean;
  mayHaveUnconfirmedChanges: boolean;
  error: string | null;
}

@Component({
  selector: 'app-bulk-job-progress',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bulk-job-progress.component.html'
})
export class BulkJobProgressComponent implements OnInit {
  readonly data: BulkJobDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<BulkJobProgressComponent>);
  private readonly jobs = inject(BackgroundJobsService);
  private readonly destroyRef = inject(DestroyRef);
  readonly jobId = signal<string | null>(null);
  readonly status = signal('Starting');
  readonly connectedMessage = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly processed = signal(0);
  readonly changed = signal(0);
  readonly unchanged = signal(0);
  readonly failed = signal(0);
  readonly progress = signal<number | null>(null);
  readonly result = signal<BulkJobResult | null>(null);
  readonly running = computed(() => ['Starting', 'Queued', 'Running', 'Cancelling'].includes(this.status()));
  readonly stopping = computed(() => this.status() === 'Cancelling');
  private lastRevision = -1;
  private resultRequested = false;
  private watchSubscription?: Subscription;

  constructor() { this.destroyRef.onDestroy(() => this.watchSubscription?.unsubscribe()); }

  ngOnInit(): void {
    this.dialogRef.disableClose = true;
    this.jobs.start('bulk-keys', this.data.connectionId, {
      operation: this.data.operation, database: this.data.database,
      pattern: this.data.pattern, keys: this.data.keys, ttlSeconds: this.data.ttlSeconds ?? null
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => { this.dialogRef.disableClose = false; this.jobId.set(job.jobId); this.watch(job.jobId); },
      error: error => { this.dialogRef.disableClose = false; this.status.set('Failed'); this.error.set(error.error?.detail || error.error?.message || 'Could not start the operation.'); }
    });
  }

  stop(): void {
    const id = this.jobId();
    if (!id) return;
    this.jobs.cancel(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => this.error.set('Could not request cancellation. Refresh status and try again.')
    });
  }

  refreshStatus(): void {
    const id = this.jobId();
    if (id) this.jobs.status(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => this.error.set('Job status is unavailable or has expired. Refresh the key list before retrying.')
    });
  }

  close(): void { this.dialogRef.close(this.result()); }

  private watch(id: string): void {
    this.watchSubscription?.unsubscribe();
    this.watchSubscription = this.jobs.watch(id).subscribe(event => {
      this.connectedMessage.set(event.connected ? null : (event.error || 'Reconnecting live updates…'));
      if (event.job) this.applyJob(event.job);
    });
  }

  private applyJob(job: BackgroundJob): void {
    if (job.revision < this.lastRevision) return;
    this.lastRevision = job.revision;
    this.status.set(job.status);
    this.progress.set(job.progressPercent);
    if (job.metrics) {
      const counts = job.metrics as Partial<BulkJobResult>;
      this.processed.set(counts.processed ?? 0);
      this.changed.set(counts.changed ?? 0);
      this.unchanged.set(counts.unchanged ?? 0);
      this.failed.set(counts.failed ?? 0);
    }
    if (job.status === 'Failed') this.error.set(job.error || 'The operation failed.');
    if (job.hasResult && !this.resultRequested) {
      this.resultRequested = true;
      this.jobs.result<BulkJobResult>(job.jobId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.result.set(result);
          this.processed.set(result.processed);
          this.changed.set(result.changed);
          this.unchanged.set(result.unchanged);
          this.failed.set(result.failed);
        },
        error: () => { this.resultRequested = false; this.error.set('Could not retrieve the result. Refresh status to retry.'); }
      });
    }
  }
}
