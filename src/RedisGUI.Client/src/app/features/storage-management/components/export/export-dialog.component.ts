import { Component, computed, inject, signal, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
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
import { BackgroundJobsStore } from '../../../../core/stores/background-jobs.store';
import { ExportJobProgress, isJobActive } from '../../../../core/models/background-jobs.types';
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
  private readonly jobsStore = inject(BackgroundJobsStore);
  private readonly destroyRef = inject(DestroyRef);
  readonly dialogRef = inject(MatDialogRef<ExportDialogComponent>);
  readonly data = inject<{
    connectionId: string;
    jobId?: string | undefined;
    format?: 'json' | 'csv' | undefined;
    database: number;
    pattern?: string | undefined;
    keys?: string[] | undefined;
    keysBase64?: (string | null)[] | undefined;
  }>(MAT_DIALOG_DATA);

  readonly selectedKeysCount = computed(() => this.data.keys?.length ?? 0);

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

  readonly jobId = signal<string | null>(
    this.data.jobId ??
      this.jobsStore
        .activeJobs()
        .find(e => e.job.kind === 'redis-export' && e.job.connectionId === this.data.connectionId)?.job
        .jobId ??
      null
  );
  private readonly starting = signal(false);
  private readonly localError = signal<string | null>(null);
  private jobFormat: 'json' | 'csv' = this.data.format ?? 'json';

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
  readonly canDownload = computed(() => this.entry()?.job.hasResult ?? false);
  readonly exportedCount = computed(() => {
    const metrics = this.entry()?.job.metrics as ExportJobProgress | null;
    return metrics?.exportedCount ?? 0;
  });
  readonly error = computed(
    () => this.localError() ?? this.entry()?.expiredReason ?? this.entry()?.job.error ?? null
  );
  readonly connectionStatus = computed(() =>
    this.jobsStore.liveConnected() ? null : (this.jobsStore.connectionNotice() || 'Reconnecting live updates…')
  );

  constructor() {
    const initialId = this.jobId();
    if (initialId && !this.entry()) {
      this.jobsStore.refreshJob(initialId);
    } else if (initialId) {
      this.jobFormat = this.jobsStore.findExportFormat(initialId);
    }
  }

  onExport(): void {
    this.jobId.set(null);
    this.localError.set(null);
    this.starting.set(true);
    this.dialogRef.disableClose = true;
    this.jobFormat = this.exportModel().format as 'json' | 'csv';

    this.jobsStore
      .startJob(
        'redis-export',
        this.data.connectionId,
        {
          format: this.jobFormat,
          pattern: this.data.pattern || '*',
          database: this.data.database,
          ...(this.data.keys?.length ? { keys: this.data.keys } : {}),
          ...(this.data.keysBase64?.length ? { keysBase64: this.data.keysBase64 } : {})
        },
        { format: this.jobFormat, database: this.data.database, markSeen: true }
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
          this.localError.set(err?.error?.detail || err?.message || 'Could not start export.');
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

  download(): void {
    const id = this.jobId();
    if (!id) return;
    const format = this.entry()?.format ?? this.jobFormat;
    this.exportService.downloadExport(id, format).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.localError.set('Could not download the export. Please try again.')
    });
  }
}
