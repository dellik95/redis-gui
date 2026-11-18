import { LoadTestProgressView } from './load-test-progress';
import { LoadTestResults } from './load-test-results';
import {
  Component,
  inject,
  computed,
  signal,
  ChangeDetectionStrategy,
  DestroyRef
} from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  LoadTestRequest,
  LoadTestResult,
  LoadTestProgress
} from '../../../../core/services/redis-cli.service';
import { BackgroundJobsStore } from '../../../../core/stores/background-jobs.store';
import { isJobActive } from '../../../../core/models/background-jobs.types';
import { NumberPresetOption, NumberPresetPickerComponent } from '../../../../shared/components';
import { UiButtonComponent } from '../../../../shared/ui';

const DURATION_PRESETS: readonly NumberPresetOption[] = [
  { value: 5, label: '5 s' },
  { value: 10, label: '10 s' },
  { value: 30, label: '30 s' },
  { value: 60, label: '1 min' },
  { value: 120, label: '2 min' },
  { value: 300, label: '5 min' }
];
const OPERATION_PRESETS: readonly NumberPresetOption[] = [
  { value: 200, label: '200' },
  { value: 500, label: '500' },
  { value: 1000, label: '1,000' },
  { value: 2500, label: '2,500' },
  { value: 5000, label: '5,000' },
  { value: 10000, label: '10,000' }
];
const CONCURRENCY_PRESETS: readonly NumberPresetOption[] = [
  { value: 1, label: '1 client' },
  { value: 5, label: '5 clients' },
  { value: 10, label: '10 clients' },
  { value: 20, label: '20 clients' },
  { value: 50, label: '50 clients' }
];
const PAYLOAD_PRESETS: readonly NumberPresetOption[] = [
  { value: 64, label: '64 B' },
  { value: 128, label: '128 B (Standard)' },
  { value: 512, label: '512 B' },
  { value: 1024, label: '1 KB' }
];

export interface LoadTestDialogData {
  connectionId: string;
  database: number;
  jobId?: string | undefined;
}

@Component({
  selector: 'app-load-test-dialog',
  standalone: true,
  imports: [
    LoadTestProgressView,
    LoadTestResults,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    NumberPresetPickerComponent,
    UiButtonComponent
  ],
  templateUrl: './load-test-dialog.component.html',
  styleUrl: './load-test-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadTestDialogComponent {
  readonly data: LoadTestDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<LoadTestDialogComponent>);
  private readonly jobsStore = inject(BackgroundJobsStore);
  private readonly destroyRef = inject(DestroyRef);

  readonly benchmarkMode = signal<'duration' | 'count'>('duration');
  readonly durationSeconds = signal<number>(10);
  readonly operationsCount = signal<number>(1000);
  readonly concurrency = signal<number>(5);
  readonly operationType = signal<'mixed' | 'write' | 'read'>('mixed');
  readonly payloadSize = signal<number>(128);
  readonly durationPresets = DURATION_PRESETS;
  readonly operationPresets = OPERATION_PRESETS;
  readonly concurrencyPresets = CONCURRENCY_PRESETS;
  readonly payloadPresets = PAYLOAD_PRESETS;

  readonly activeJobId = signal<string | null>(
    this.data.jobId ??
      this.jobsStore
        .activeJobs()
        .find(e => e.job.kind === 'load-test' && e.job.connectionId === this.data.connectionId)?.job
        .jobId ??
      null
  );
  private readonly starting = signal(false);
  private readonly localError = signal<string | null>(null);

  private readonly entry = computed(() => {
    const id = this.activeJobId();
    return id ? (this.jobsStore.jobsById()[id] ?? null) : null;
  });

  readonly isRunning = computed(() => {
    if (this.starting()) return true;
    const status = this.entry()?.job.status;
    return status ? isJobActive(status) : false;
  });

  readonly isStopping = computed(() => this.entry()?.job.status === 'Cancelling');

  readonly liveProgress = computed<LoadTestProgress | null>(
    () => (this.entry()?.job.metrics as LoadTestProgress | null) ?? null
  );

  readonly result = computed<LoadTestResult | null>(
    () => (this.entry()?.resultData as LoadTestResult | null) ?? null
  );

  readonly errorMessage = computed(() => {
    if (this.localError()) return this.localError();
    const current = this.entry();
    if (!current) return null;
    if (current.expiredReason) return current.expiredReason;
    if (current.job.status === 'Cancelled' && !current.job.hasResult) {
      return 'Load test cancelled.';
    }
    return current.job.error ?? current.resultError ?? null;
  });

  readonly connectionStatus = computed(() =>
    this.jobsStore.liveConnected() ? null : (this.jobsStore.connectionNotice() || 'Reconnecting live updates…')
  );

  readonly progressPercent = computed(() => this.entry()?.job.progressPercent ?? 0);

  constructor() {
    const initialId = this.activeJobId();
    if (initialId && !this.entry()) {
      this.jobsStore.refreshJob(initialId);
    }
  }

  startLoadTest(): void {
    this.dialogRef.disableClose = true;
    this.activeJobId.set(null);
    this.starting.set(true);
    this.localError.set(null);

    const isDuration = this.benchmarkMode() === 'duration';
    const request: LoadTestRequest = {
      operationsCount: isDuration ? 0 : this.operationsCount(),
      durationSeconds: isDuration ? this.durationSeconds() : 0,
      concurrency: this.concurrency(),
      operationType: this.operationType(),
      payloadSizeBytes: this.payloadSize(),
      database: this.data.database,
      keyPrefix: '__bench_'
    };

    this.jobsStore
      .startJob('load-test', this.data.connectionId, request, {
        database: this.data.database,
        markSeen: true
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: job => {
          this.starting.set(false);
          this.dialogRef.disableClose = false;
          this.activeJobId.set(job.jobId);
        },
        error: error => {
          this.starting.set(false);
          this.dialogRef.disableClose = false;
          this.localError.set(error?.error?.detail || error?.message || 'Could not start the job.');
        }
      });
  }

  stopLoadTest(): void {
    const jobId = this.activeJobId();
    if (!jobId) return;

    this.jobsStore
      .cancelJob(jobId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.localError.set('Could not request cancellation.')
      });
  }

  reset(): void {
    this.activeJobId.set(null);
    this.localError.set(null);
  }
}
