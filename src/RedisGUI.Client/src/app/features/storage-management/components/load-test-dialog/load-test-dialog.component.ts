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
import { BackgroundJob, BackgroundJobsService } from '../../../../core/services/background-jobs.service';
import { Subscription } from 'rxjs';
import { NumberPresetOption, NumberPresetPickerComponent } from '../../../../shared/components';

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
    NumberPresetPickerComponent
  ],
  templateUrl: './load-test-dialog.component.html',
  styleUrl: './load-test-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadTestDialogComponent {
  readonly data: LoadTestDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<LoadTestDialogComponent>);
  private readonly jobs = inject(BackgroundJobsService);
  private readonly destroyRef = inject(DestroyRef);

  // Form options
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

  // Execution state
  readonly isRunning = signal<boolean>(false);
  readonly isStopping = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly liveProgress = signal<LoadTestProgress | null>(null);
  readonly result = signal<LoadTestResult | null>(null);
  readonly connectionStatus = signal<string | null>(null);
  readonly activeJobId = signal<string | null>(null);
  private readonly jobProgress = signal(0);

  private watchSubscription?: Subscription;
  private resultRequested = false;
  private lastRevision = -1;

  constructor() {
    const saved = sessionStorage.getItem(this.storageKey());
    if (saved) { this.activeJobId.set(saved); this.isRunning.set(true); this.watch(saved); }
    this.destroyRef.onDestroy(() => this.watchSubscription?.unsubscribe());
  }

  startLoadTest(): void {
    this.dialogRef.disableClose = true;
    this.watchSubscription?.unsubscribe();
    this.activeJobId.set(null);
    sessionStorage.removeItem(this.storageKey());
    this.isRunning.set(true);
    this.isStopping.set(false);
    this.errorMessage.set(null);
    this.result.set(null);
    this.liveProgress.set(null);

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

    this.resultRequested = false;
    this.lastRevision = -1;
    this.jobs.start('load-test', this.data.connectionId, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => {
        this.dialogRef.disableClose = false;
        this.activeJobId.set(job.jobId);
        sessionStorage.setItem(this.storageKey(), job.jobId);
        this.watch(job.jobId);
      },
      error: error => { this.dialogRef.disableClose = false; this.isRunning.set(false); this.errorMessage.set(error.error?.detail || error.message || 'Could not start the job.'); }
    });
  }

  stopLoadTest(): void {
    const jobId = this.activeJobId();
    if (!jobId) return;
    this.isStopping.set(true);

    this.jobs
      .cancel(jobId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: job => this.applyJob(job),
        error: () => {
          this.isStopping.set(false);
          this.connectionStatus.set('Could not request cancellation. Refresh status and try again.');
        }
      });
  }

  reset(): void {
    this.watchSubscription?.unsubscribe();
    this.activeJobId.set(null);
    sessionStorage.removeItem(this.storageKey());
    this.resultRequested = false;
    this.lastRevision = -1;
    this.result.set(null);
    this.liveProgress.set(null);
    this.errorMessage.set(null);
    this.isStopping.set(false);
  }

  refreshStatus(): void {
    const id = this.activeJobId();
    if (id) this.jobs.status(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => { this.isRunning.set(false); this.errorMessage.set('Job status is unavailable or has expired.'); }
    });
  }

  private watch(jobId: string): void {
    this.watchSubscription?.unsubscribe();
    this.watchSubscription = this.jobs.watch(jobId).subscribe(event => {
      this.connectionStatus.set(event.connected ? null : (event.error || 'Reconnecting live updates…'));
      if (event.job) this.applyJob(event.job);
    });
  }

  private applyJob(job: BackgroundJob): void {
    if (job.revision < this.lastRevision) return;
    this.lastRevision = job.revision;
    this.jobProgress.set(job.progressPercent ?? 0);
    if (job.metrics) this.liveProgress.set(job.metrics as LoadTestProgress);
    this.isStopping.set(job.status === 'Cancelling');
    this.isRunning.set(job.status === 'Queued' || job.status === 'Running' || job.status === 'Cancelling');
    if (job.status === 'Failed') this.errorMessage.set(job.error || 'Load test failed.');
    if (job.status === 'Cancelled' && !job.hasResult) this.errorMessage.set('Load test cancelled.');
    if (job.hasResult && !this.resultRequested) {
      this.resultRequested = true;
      this.jobs.result<LoadTestResult>(job.jobId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => this.result.set(result),
        error: () => { this.resultRequested = false; this.errorMessage.set('Could not retrieve the job result.'); }
      });
    }
  }

  private storageKey(): string { return `redisgui:load-test:${this.data.connectionId}:${this.data.database}`; }
  readonly progressPercent = computed(() => this.jobProgress());
}
