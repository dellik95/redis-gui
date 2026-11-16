import {
  Component,
  inject,
  signal,
  computed,
  DestroyRef,
  ChangeDetectionStrategy
} from '@angular/core';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogTitle,
  MatDialogContent,
  MatDialogActions
} from '@angular/material/dialog';

import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ImportResult, ImportService } from '../../services/import.service';
import { BackgroundJob, BackgroundJobsService } from '../../../../core/services/background-jobs.service';
import { Subscription } from 'rxjs';

/**
 * Import Dialog Component
 * Dialog for importing Redis data from file
 *
 * Features:
 * - File selection (JSON/CSV)
 * - Import progress indicator
 * - Error handling
 * - Automatic validation
 *
 * @example
 * ```typescript
 * this.dialog.open(ImportDialogComponent, {
 *   data: { connectionId: 'conn-123' }
 * });
 * ```
 */
@Component({
  selector: 'app-import-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatProgressSpinnerModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions
  ],
  templateUrl: './import-dialog.component.html',
  styleUrl: './import-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ImportDialogComponent {
  // 1. Injected dependencies
  private readonly importService = inject(ImportService);
  private readonly jobs = inject(BackgroundJobsService);
  private readonly destroyRef = inject(DestroyRef);
  readonly dialogRef = inject(MatDialogRef<ImportDialogComponent>);
  readonly data = inject<{ connectionId: string; database: number }>(MAT_DIALOG_DATA);

  // 2. State signals
  /** Selected file for import */
  readonly file = signal<File | null>(null);

  /** Loading state during import */
  readonly isLoading = signal<boolean>(false);

  /** Error message if import fails */
  readonly error = signal<string | null>(null);
  readonly jobId = signal<string | null>(null);
  readonly jobStatus = signal<string | null>(null);
  readonly connectionStatus = signal<string | null>(null);
  readonly processed = signal(0);
  readonly total = signal(0);
  readonly result = signal<ImportResult | null>(null);
  readonly stopping = signal(false);
  private watchSubscription?: Subscription;
  private resultRequested = false;
  private lastRevision = -1;

  constructor() {
    const saved = sessionStorage.getItem(this.storageKey());
    if (saved) { this.jobId.set(saved); this.isLoading.set(true); this.watch(saved); }
    this.destroyRef.onDestroy(() => this.watchSubscription?.unsubscribe());
  }

  // 3. Computed signals
  /** Whether import can be executed */
  readonly canImport = computed(() => {
    return this.file() !== null && !this.isLoading();
  });

  /**
   * Handle file selection
   * @param event - File input change event
   */
  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input?.files && input.files.length > 0) {
      const selectedFile = input.files[0];
      this.file.set(selectedFile ?? null);
      this.error.set(null);
    }
  }

  /**
   * Handle import action
   * Imports the selected file to Redis
   */
  onImport(): void {
    const fileValue = this.file();
    if (!fileValue) return;

    this.error.set(null);
    this.isLoading.set(true);
    this.dialogRef.disableClose = true;
    this.watchSubscription?.unsubscribe();
    this.jobId.set(null);
    this.result.set(null);
    this.jobStatus.set(null);
    this.connectionStatus.set(null);
    this.processed.set(0);
    this.total.set(0);
    this.resultRequested = false;
    this.lastRevision = -1;
    sessionStorage.removeItem(this.storageKey());

    this.importService
      .importData(this.data.connectionId, fileValue, this.data.database)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: job => {
          this.dialogRef.disableClose = false;
          this.jobId.set(job.jobId);
          sessionStorage.setItem(this.storageKey(), job.jobId);
          this.watch(job.jobId);
        },
        error: (err: Error) => {
          this.dialogRef.disableClose = false;
          this.isLoading.set(false);
          this.error.set(err.message || 'Import failed. Ensure file format is correct.');
        }
      });
  }

  cancel(): void {
    const id = this.jobId();
    if (!id) return;
    this.stopping.set(true);
    this.jobs.cancel(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => { this.stopping.set(false); this.error.set('Could not request cancellation.'); }
    });
  }

  refreshStatus(): void {
    const id = this.jobId();
    if (id) this.jobs.status(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => { this.isLoading.set(false); this.error.set('Job status is unavailable or has expired. Refresh the keys before retrying.'); }
    });
  }

  close(): void { this.dialogRef.close(this.result()); }

  private watch(id: string): void {
    this.watchSubscription?.unsubscribe();
    this.watchSubscription = this.jobs.watch(id).subscribe(event => {
      this.connectionStatus.set(event.connected ? null : (event.error || 'Reconnecting live updates…'));
      if (event.job) this.applyJob(event.job);
    });
  }

  private applyJob(job: BackgroundJob): void {
    if (job.revision < this.lastRevision) return;
    this.lastRevision = job.revision;
    this.jobStatus.set(job.status);
    this.isLoading.set(['Queued', 'Running', 'Cancelling'].includes(job.status));
    this.stopping.set(job.status === 'Cancelling');
    if (job.metrics) {
      const metrics = job.metrics as { processed: number; total: number };
      this.processed.set(metrics.processed);
      this.total.set(metrics.total);
    }
    if (job.status === 'Failed') this.error.set(job.error || 'Import failed.');
    if (job.status === 'Cancelled' && !job.hasResult) this.error.set('Import cancelled before a result was available. Refresh the keys before retrying.');
    if (job.hasResult && !this.resultRequested) {
      this.resultRequested = true;
      this.jobs.result<ImportResult>(job.jobId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (this.jobId() !== job.jobId) return;
          this.result.set(result);
          this.processed.set(result.importedCount + result.skippedCount + result.failedCount);
        },
        error: () => { if (this.jobId() === job.jobId) { this.resultRequested = false; this.error.set('Could not retrieve the import result. Refresh status to retry.'); } }
      });
    }
  }

  private storageKey(): string { return `redisgui:import:${this.data.connectionId}:${this.data.database}`; }
}
