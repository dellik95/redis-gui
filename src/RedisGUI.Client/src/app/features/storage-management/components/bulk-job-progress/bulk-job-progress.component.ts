import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  BulkKeyJobProgress,
  BulkKeyJobResult,
  isJobActive
} from '../../../../core/models/background-jobs.types';
import { BackgroundJobsStore } from '../../../../core/stores/background-jobs.store';
import { UiButtonComponent } from '../../../../shared/ui';

export interface BulkJobDialogData {
  connectionId: string;
  jobId?: string | undefined;
  database?: number | undefined;
  operation?: 'delete-pattern' | 'delete' | 'ttl' | undefined;
  pattern?: string | undefined;
  keys?: string[] | undefined;
  keysBase64?: (string | null)[] | undefined;
  ttlSeconds?: number | null | undefined;
}

@Component({
  selector: 'app-bulk-job-progress',
  standalone: true,
  imports: [MatDialogModule, UiButtonComponent, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bulk-job-progress.component.html'
})
export class BulkJobProgressComponent implements OnInit {
  readonly data: BulkJobDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<BulkJobProgressComponent>);
  private readonly jobsStore = inject(BackgroundJobsStore);
  private readonly destroyRef = inject(DestroyRef);

  readonly jobId = signal<string | null>(this.data.jobId ?? null);
  private readonly starting = signal(false);
  private readonly localError = signal<string | null>(null);

  private readonly entry = computed(() => {
    const id = this.jobId();
    return id ? (this.jobsStore.jobsById()[id] ?? null) : null;
  });

  readonly status = computed(() => {
    const current = this.entry();
    if (current) return current.job.status;
    if (this.localError()) return 'Failed';
    return this.starting() ? 'Starting' : 'Queued';
  });

  readonly connectedMessage = computed(() =>
    this.jobsStore.liveConnected() ? null : (this.jobsStore.connectionNotice() || 'Reconnecting live updates…')
  );

  readonly error = computed(() => {
    if (this.localError()) return this.localError();
    const current = this.entry();
    return current?.expiredReason ?? current?.job.error ?? current?.resultError ?? null;
  });

  readonly result = computed<BulkKeyJobResult | null>(() => {
    const current = this.entry();
    return (current?.resultData as BulkKeyJobResult | null) ?? null;
  });

  readonly processed = computed(() => {
    const outcome = this.result();
    if (outcome) return outcome.processed;
    const metrics = this.entry()?.job.metrics as BulkKeyJobProgress | null;
    return metrics?.processed ?? 0;
  });

  readonly changed = computed(() => {
    const outcome = this.result();
    if (outcome) return outcome.changed;
    const metrics = this.entry()?.job.metrics as BulkKeyJobProgress | null;
    return metrics?.changed ?? 0;
  });

  readonly unchanged = computed(() => {
    const outcome = this.result();
    if (outcome) return outcome.unchanged;
    const metrics = this.entry()?.job.metrics as BulkKeyJobProgress | null;
    return metrics?.unchanged ?? 0;
  });

  readonly failed = computed(() => {
    const outcome = this.result();
    if (outcome) return outcome.failed;
    const metrics = this.entry()?.job.metrics as BulkKeyJobProgress | null;
    return metrics?.failed ?? 0;
  });

  readonly progress = computed<number | null>(() => {
    const status = this.status();
    if (status === 'Completed') return 100;
    return this.entry()?.job.progressPercent ?? null;
  });

  readonly running = computed(() => {
    if (this.starting()) return true;
    const s = this.status();
    return s === 'Starting' || isJobActive(s as Parameters<typeof isJobActive>[0]);
  });

  readonly stopping = computed(() => this.status() === 'Cancelling');

  ngOnInit(): void {
    if (this.data.jobId && !this.data.operation) {
      if (!this.entry()) {
        this.jobsStore.refreshJob(this.data.jobId);
      }
      return;
    }

    if (!this.data.operation) {
      this.localError.set('No bulk operation was specified.');
      return;
    }

    this.starting.set(true);
    this.dialogRef.disableClose = true;
    this.jobsStore
      .startJob(
        'bulk-keys',
        this.data.connectionId,
        {
          operation: this.data.operation,
          database: this.data.database ?? 0,
          pattern: this.data.pattern,
          keys: this.data.keys,
          keysBase64: this.data.keysBase64,
          ttlSeconds: this.data.ttlSeconds ?? null
        },
        { database: this.data.database ?? 0, markSeen: true }
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: job => {
          this.starting.set(false);
          this.dialogRef.disableClose = false;
          this.jobId.set(job.jobId);
        },
        error: err => {
          this.starting.set(false);
          this.dialogRef.disableClose = false;
          this.localError.set(err?.error?.detail || err?.error?.message || 'Could not start the operation.');
        }
      });
  }

  stop(): void {
    const id = this.jobId();
    if (!id) return;
    this.jobsStore.cancelJob(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      error: () => this.localError.set('Could not request cancellation.')
    });
  }

  close(): void {
    this.dialogRef.close(this.result());
  }
}
