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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ImportResult, ImportService } from '../../services/import.service';
import { BackgroundJobsStore } from '../../../../core/stores/background-jobs.store';
import { ImportJobProgress, isJobActive } from '../../../../core/models/background-jobs.types';
import { UiButtonComponent, UiCheckboxComponent } from '../../../../shared/ui';

/**
 * Import Dialog Component
 * Dialog for importing Redis data from file
 */
@Component({
  selector: 'app-import-dialog',
  standalone: true,
  imports: [
    UiButtonComponent,
    UiCheckboxComponent,
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
  private readonly importService = inject(ImportService);
  private readonly jobsStore = inject(BackgroundJobsStore);
  private readonly destroyRef = inject(DestroyRef);
  readonly dialogRef = inject(MatDialogRef<ImportDialogComponent>);
  readonly data = inject<{
    connectionId: string;
    database: number;
    jobId?: string | undefined;
  }>(MAT_DIALOG_DATA);

  readonly file = signal<File | null>(null);
  readonly overwrite = signal(false);
  readonly jobId = signal<string | null>(
    this.data.jobId ??
      this.jobsStore
        .activeJobs()
        .find(e => e.job.kind === 'redis-import' && e.job.connectionId === this.data.connectionId)?.job
        .jobId ??
      null
  );
  private readonly starting = signal(false);
  private readonly localError = signal<string | null>(null);

  private readonly entry = computed(() => {
    const id = this.jobId();
    return id ? (this.jobsStore.jobsById()[id] ?? null) : null;
  });

  readonly jobStatus = computed(() => this.entry()?.job.status ?? null);
  readonly isLoading = computed(() => {
    if (this.starting()) return true;
    const status = this.entry()?.job.status;
    return status ? isJobActive(status) : false;
  });
  readonly stopping = computed(() => this.entry()?.job.status === 'Cancelling');
  readonly result = computed<ImportResult | null>(
    () => (this.entry()?.resultData as ImportResult | null) ?? null
  );
  readonly processed = computed(() => {
    const res = this.result();
    if (res) return res.importedCount + res.skippedCount + res.failedCount;
    const metrics = this.entry()?.job.metrics as ImportJobProgress | null;
    return metrics?.processed ?? 0;
  });
  readonly total = computed(() => {
    const metrics = this.entry()?.job.metrics as ImportJobProgress | null;
    return metrics?.total ?? 0;
  });
  readonly error = computed(() => {
    if (this.localError()) return this.localError();
    const current = this.entry();
    if (!current) return null;
    if (current.expiredReason) return current.expiredReason;
    if (current.job.status === 'Cancelled' && !current.job.hasResult) {
      return 'Import cancelled before a result was available. Refresh the keys before retrying.';
    }
    return current.job.error ?? current.resultError ?? null;
  });
  readonly connectionStatus = computed(() =>
    this.jobsStore.liveConnected() ? null : (this.jobsStore.connectionNotice() || 'Reconnecting live updates…')
  );

  readonly canImport = computed(() => this.file() !== null && !this.isLoading());

  constructor() {
    const initialId = this.jobId();
    if (initialId && !this.entry()) {
      this.jobsStore.refreshJob(initialId);
    }
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input?.files && input.files.length > 0) {
      const selectedFile = input.files[0];
      this.file.set(selectedFile ?? null);
      this.localError.set(null);
    }
  }

  onImport(): void {
    const fileValue = this.file();
    if (!fileValue) return;

    this.localError.set(null);
    this.starting.set(true);
    this.dialogRef.disableClose = true;
    this.jobId.set(null);

    this.importService
      .importData(this.data.connectionId, fileValue, this.data.database, this.overwrite())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: job => {
          this.starting.set(false);
          this.dialogRef.disableClose = false;
          this.jobId.set(job.jobId);
          this.jobsStore.trackJob(job, { database: this.data.database, markSeen: true });
        },
        error: (err: Error) => {
          this.starting.set(false);
          this.dialogRef.disableClose = false;
          this.localError.set(err.message || 'Import failed. Ensure file format is correct.');
        }
      });
  }

  cancel(): void {
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
