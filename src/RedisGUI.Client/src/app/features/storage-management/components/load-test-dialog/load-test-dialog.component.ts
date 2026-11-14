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
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  RedisCliService,
  LoadTestRequest,
  LoadTestResult,
  LoadTestProgress
} from '../../../../core/services/redis-cli.service';
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
  private readonly cliService = inject(RedisCliService);
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

  private activeTestId: string | null = null;
  private abortController: AbortController | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.abortController) {
        this.abortController.abort();
      }
    });
  }

  startLoadTest(): void {
    this.isRunning.set(true);
    this.isStopping.set(false);
    this.errorMessage.set(null);
    this.result.set(null);
    this.liveProgress.set(null);

    const testId = 'bench_' + Date.now();
    this.activeTestId = testId;

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

    this.abortController = this.cliService.streamLoadTest(
      this.data.connectionId,
      request,
      testId,
      progress => {
        this.liveProgress.set(progress);
      },
      res => {
        this.isRunning.set(false);
        this.isStopping.set(false);
        this.result.set(res);
        this.activeTestId = null;
        this.abortController = null;
      },
      err => {
        this.isRunning.set(false);
        this.isStopping.set(false);
        this.errorMessage.set(err);
        this.activeTestId = null;
        this.abortController = null;
      }
    );
  }

  stopLoadTest(): void {
    if (!this.activeTestId) return;
    this.isStopping.set(true);

    this.cliService
      .stopLoadTest(this.data.connectionId, this.activeTestId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Progress stream will receive completed event and end gracefully
        },
        error: () => {
          this.isStopping.set(false);
        }
      });
  }

  reset(): void {
    this.result.set(null);
    this.liveProgress.set(null);
    this.errorMessage.set(null);
    this.isStopping.set(false);
  }

  readonly progressPercent = computed(() => {
    const p = this.liveProgress();
    if (!p) return 0;

    if (this.benchmarkMode() === 'duration') {
      const dur = this.durationSeconds();
      if (!dur || dur <= 0) return 0;
      return Math.min(100, (p.elapsedSeconds / dur) * 100);
    } else {
      const total = this.operationsCount();
      if (!total || total <= 0) return 0;
      return Math.min(100, (p.completedOperations / total) * 100);
    }
  });
}
