import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BackgroundJobsStore } from '../../../core/stores/background-jobs.store';
import { ExportService } from '../../../features/storage-management/services/export.service';
import {
  BackgroundJob,
  TrackedJobEntry,
  describeJobDisplayName,
  isBulkKeysJob,
  isExportJob,
  isImportJob,
  isJobActive,
  isLoadTestJob,
  isSeedDataJob
} from '../../../core/models/background-jobs.types';

@Component({
  selector: 'app-job-notification-center',
  standalone: true,
  imports: [
    DatePipe,
    MatBadgeModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressBarModule,
    MatTooltipModule
  ],
  templateUrl: './job-notification-center.component.html',
  styleUrl: './job-notification-center.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JobNotificationCenterComponent {
  readonly jobsStore = inject(BackgroundJobsStore);
  private readonly dialog = inject(MatDialog);
  private readonly exportService = inject(ExportService);

  labelFor(job: BackgroundJob): string {
    return describeJobDisplayName(job);
  }

  isActive(entry: TrackedJobEntry): boolean {
    return isJobActive(entry.job.status);
  }

  formatMetricsSummary(entry: TrackedJobEntry): string | null {
    const job = entry.job;
    if (isBulkKeysJob(job) && job.metrics) {
      const m = job.metrics;
      return `Processed ${m.processed}${m.total !== null ? ` / ${m.total}` : ''} · Changed ${m.changed}`;
    }
    if (isExportJob(job) && job.metrics) {
      return `Exported ${job.metrics.exportedCount} keys`;
    }
    if (isImportJob(job) && job.metrics) {
      return `Imported ${job.metrics.imported} · Skipped ${job.metrics.skipped} · Failed ${job.metrics.failed}`;
    }
    if (isLoadTestJob(job) && job.metrics) {
      return `${job.metrics.completedOperations} / ${job.metrics.totalOperations} ops · ${Math.round(job.metrics.currentOpsPerSecond)} ops/s`;
    }
    if (isSeedDataJob(job) && job.metrics) {
      const m = job.metrics;
      if (m.failed && m.failed > 0) {
        return `Seeded ${m.created} / ${m.total} keys (${m.failed} failed)`;
      }
      return `Seeded ${m.created} / ${m.total} keys`;
    }
    return null;
  }

  stopJob(event: MouseEvent, jobId: string): void {
    event.stopPropagation();
    this.jobsStore.cancelJob(jobId).subscribe();
  }

  downloadResult(event: MouseEvent, entry: TrackedJobEntry): void {
    event.stopPropagation();
    this.exportService.downloadExport(entry.job.jobId, entry.format ?? 'json').subscribe({
      error: (err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === 404) {
          this.jobsStore.markJobExpired(
            entry.job.jobId,
            'The exported file has expired and was cleaned up. Run the export again.'
          );
        }
      }
    });
  }

  dismiss(event: MouseEvent, jobId: string): void {
    event.stopPropagation();
    this.jobsStore.dismissJob(jobId);
  }

  clearCompleted(event: MouseEvent): void {
    event.stopPropagation();
    this.jobsStore.clearCompleted();
  }

  async openDetails(entry: TrackedJobEntry): Promise<void> {
    const job = entry.job;
    const common = {
      jobId: job.jobId,
      connectionId: job.connectionId,
      database: entry.database ?? 0
    };

    switch (job.kind) {
      case 'bulk-keys': {
        const { BulkJobProgressComponent } =
          await import('../../../features/storage-management/components/bulk-job-progress/bulk-job-progress.component');
        this.dialog.open(BulkJobProgressComponent, { width: '500px', data: common });
        break;
      }
      case 'redis-export': {
        const { ExportDialogComponent } =
          await import('../../../features/storage-management/components/export/export-dialog.component');
        this.dialog.open(ExportDialogComponent, {
          width: '480px',
          data: { ...common, format: entry.format ?? 'json', pattern: '*' }
        });
        break;
      }
      case 'redis-import': {
        const { ImportDialogComponent } =
          await import('../../../features/storage-management/components/import/import-dialog.component');
        this.dialog.open(ImportDialogComponent, { width: '480px', data: common });
        break;
      }
      case 'load-test': {
        const { LoadTestDialogComponent } =
          await import('../../../features/storage-management/components/load-test-dialog/load-test-dialog.component');
        this.dialog.open(LoadTestDialogComponent, { width: '680px', data: common });
        break;
      }
      case 'seed-data': {
        const { SeedDataDialogComponent } =
          await import('../../../features/storage-management/components/seed-data-dialog/seed-data-dialog.component');
        this.dialog.open(SeedDataDialogComponent, { width: '680px', data: common });
        break;
      }
      default:
        break;
    }
  }
}
