import { Component, inject, signal, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogTitle,
  MatDialogContent,
  MatDialogActions
} from '@angular/material/dialog';
import { form, FormField } from '@angular/forms/signals';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ExportService } from '../../services/export.service';
import { BackgroundJob, BackgroundJobsService } from '../../../../core/services/background-jobs.service';
import { Subscription } from 'rxjs';
import { UiRadioGroupComponent, UiRadioOption, UiButtonComponent } from '../../../../shared/ui';

/**
 * Export Dialog Component
 * Dialog for exporting Redis data using Signal Forms and UiRadioGroup
 */
@Component({
  selector: 'app-export-dialog',
  standalone: true,
  imports: [
    FormField,
    MatProgressSpinnerModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    UiRadioGroupComponent,
    UiButtonComponent
  ],
  templateUrl: './export-dialog.component.html',
  styleUrl: './export-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExportDialogComponent {
  private readonly exportService = inject(ExportService);
  private readonly jobs = inject(BackgroundJobsService);
  private readonly destroyRef = inject(DestroyRef);
  readonly dialogRef = inject(MatDialogRef<ExportDialogComponent>);
  readonly data = inject<{ connectionId: string; format?: 'json' | 'csv'; database: number; pattern: string }>(
    MAT_DIALOG_DATA
  );

  /** Signal form model */
  readonly exportModel = signal({ format: this.data.format ?? 'json' });
  readonly exportForm = form(this.exportModel);

  readonly formatOptions: readonly UiRadioOption<'json' | 'csv'>[] = [
    {
      label: 'JSON format',
      description: 'Redis-native snapshots encoded as JSON',
      value: 'json'
    },
    {
      label: 'CSV format',
      description: 'Redis-native snapshots encoded as CSV',
      value: 'csv'
    }
  ];

  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly jobId = signal<string | null>(null);
  readonly jobStatus = signal<string | null>(null);
  readonly connectionStatus = signal<string | null>(null);
  readonly exportedCount = signal(0);
  readonly canDownload = signal(false);
  readonly stopping = signal(false);
  private jobFormat: 'json' | 'csv' = 'json';
  private lastRevision = -1;
  private watchSubscription?: Subscription;

  constructor() {
    const saved = sessionStorage.getItem(this.storageKey());
    if (saved) {
      try {
        const job = JSON.parse(saved) as { id: string; format: 'json' | 'csv' };
        this.jobId.set(job.id);
        this.jobFormat = job.format;
        this.isLoading.set(true);
        this.watch(job.id);
      } catch { sessionStorage.removeItem(this.storageKey()); }
    }
    this.destroyRef.onDestroy(() => this.watchSubscription?.unsubscribe());
  }

  onExport(): void {
    this.watchSubscription?.unsubscribe();
    this.jobId.set(null);
    this.jobStatus.set(null);
    this.exportedCount.set(0);
    this.connectionStatus.set(null);
    sessionStorage.removeItem(this.storageKey());
    this.error.set(null);
    this.isLoading.set(true);
    this.dialogRef.disableClose = true;
    this.canDownload.set(false);
    this.lastRevision = -1;
    this.jobFormat = this.exportModel().format as 'json' | 'csv';
    this.jobs.start('redis-export', this.data.connectionId, {
      format: this.jobFormat, pattern: this.data.pattern, database: this.data.database
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => {
        this.dialogRef.disableClose = false;
        this.jobId.set(job.jobId);
        sessionStorage.setItem(this.storageKey(), JSON.stringify({ id: job.jobId, format: this.jobFormat }));
        this.watch(job.jobId);
      },
      error: err => { this.dialogRef.disableClose = false; this.isLoading.set(false); this.error.set(err.error?.detail || err.message || 'Could not start export.'); }
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

  download(): void {
    const id = this.jobId();
    if (!id) return;
    this.exportService.downloadExport(id, this.jobFormat).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.error.set('Could not download the export. Refresh status and try again.')
    });
  }

  refreshStatus(): void {
    const id = this.jobId();
    if (id) this.jobs.status(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => { this.isLoading.set(false); this.error.set('Job status is unavailable or has expired.'); }
    });
  }

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
    this.canDownload.set(job.hasResult);
    if (job.metrics) this.exportedCount.set((job.metrics as { exportedCount: number }).exportedCount ?? 0);
    if (job.status === 'Failed') this.error.set(job.error || 'Export failed.');
  }

  private storageKey(): string { return `redisgui:export:${this.data.connectionId}:${this.data.database}:${this.data.pattern}`; }
}
