import { SeedResult } from './seed-result';
import { Component, inject, signal, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SeedSampleDataOptions, SeedDataResult } from '../../../../core/services/redis-cli.service';
import { BackgroundJob, BackgroundJobsService } from '../../../../core/services/background-jobs.service';
import { Subscription } from 'rxjs';
import { NumberPresetOption, NumberPresetPickerComponent } from '../../../../shared/components';

const KEY_COUNT_PRESETS: readonly NumberPresetOption[] = [
  { value: 25, label: '25' },
  { value: 50, label: '50' },
  { value: 100, label: '100' },
  { value: 250, label: '250' },
  { value: 500, label: '500' },
  { value: 1000, label: '1,000' }
];
const RANDOM_DATABASE_PRESETS: readonly NumberPresetOption[] = [
  { value: 2, label: '2 DBs' },
  { value: 4, label: '4 DBs' },
  { value: 8, label: '8 DBs' },
  { value: 16, label: '16 DBs' }
];

export interface SeedDataDialogData {
  connectionId: string;
  database: number;
}

@Component({
  selector: 'app-seed-data-dialog',
  standalone: true,
  imports: [
    SeedResult,
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    NumberPresetPickerComponent
  ],
  templateUrl: './seed-data-dialog.component.html',
  styleUrl: './seed-data-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SeedDataDialogComponent {
  readonly data: SeedDataDialogData = inject(MAT_DIALOG_DATA);
  private readonly jobs = inject(BackgroundJobsService);
  private readonly dialogRef = inject(MatDialogRef<SeedDataDialogComponent>);
  private readonly destroyRef = inject(DestroyRef);

  readonly availableDbs = Array.from({ length: 16 }, (_, i) => i);

  // Form Signals
  readonly keyCount = signal<number>(50);
  readonly dbMode = signal<'current' | 'specific' | 'random'>('current');
  readonly selectedDbs = signal<number[]>([this.data.database]);
  readonly randomDbCount = signal<number>(4);
  readonly keyCountPresets = KEY_COUNT_PRESETS;
  readonly randomDatabasePresets = RANDOM_DATABASE_PRESETS;
  readonly keyComplexity = signal<'simple' | 'hierarchical' | 'uuid' | 'hashtag'>('hierarchical');
  readonly payloadComplexity = signal<'simple' | 'json' | 'complex'>('json');
  readonly selectedTypes = signal<string[]>(['string', 'hash', 'list', 'set', 'zset', 'stream']);
  readonly ttlStrategy = signal<'mixed' | 'persistent' | 'expiring'>('mixed');
  readonly seedModel = signal({ keyPrefix: 'demo' });
  readonly seedForm = form(this.seedModel);

  // Status Signals
  readonly isSeeding = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly result = signal<SeedDataResult | null>(null);
  readonly jobId = signal<string | null>(null);
  readonly jobStatus = signal<string | null>(null);
  readonly connectionStatus = signal<string | null>(null);
  readonly created = signal(0);
  readonly total = signal(0);
  readonly stopping = signal(false);
  private watchSubscription?: Subscription;
  private resultRequested = false;
  private lastRevision = -1;

  constructor() {
    const saved = sessionStorage.getItem(this.storageKey());
    if (saved) { this.jobId.set(saved); this.isSeeding.set(true); this.watch(saved); }
    this.destroyRef.onDestroy(() => this.watchSubscription?.unsubscribe());
  }

  isDbSelected(db: number): boolean {
    return this.selectedDbs().includes(db);
  }

  toggleDbSelection(db: number): void {
    const list = [...this.selectedDbs()];
    const idx = list.indexOf(db);
    if (idx >= 0) {
      if (list.length > 1) {
        list.splice(idx, 1);
      }
    } else {
      list.push(db);
    }
    this.selectedDbs.set(list.sort((a, b) => a - b));
  }

  isTypeSelected(type: string): boolean {
    return this.selectedTypes().includes(type);
  }

  toggleType(type: string): void {
    const list = [...this.selectedTypes()];
    const idx = list.indexOf(type);
    if (idx >= 0) {
      if (list.length > 1) {
        list.splice(idx, 1);
      }
    } else {
      list.push(type);
    }
    this.selectedTypes.set(list);
  }

  getTypeIcon(type: string): string {
    switch (type.toLowerCase()) {
      case 'string':
        return 'text_fields';
      case 'hash':
        return 'data_object';
      case 'list':
        return 'format_list_numbered';
      case 'set':
        return 'category';
      case 'zset':
      case 'sortedset':
        return 'stacked_bar_chart';
      case 'stream':
        return 'water';
      default:
        return 'data_object';
    }
  }

  getTypeLabel(type: string): string {
    switch (type.toLowerCase()) {
      case 'string':
        return 'Strings';
      case 'hash':
        return 'Hashes';
      case 'list':
        return 'Lists';
      case 'set':
        return 'Sets';
      case 'zset':
      case 'sortedset':
        return 'Sorted Sets';
      case 'stream':
        return 'Streams';
      default:
        return type;
    }
  }

  getTargetDbDescription(): string {
    if (this.dbMode() === 'current') return `DB ${this.data.database}`;
    if (this.dbMode() === 'specific') return `DBs [${this.selectedDbs().join(', ')}]`;
    return `${this.randomDbCount()} random DBs`;
  }

  startSeed(): void {
    this.watchSubscription?.unsubscribe();
    this.jobId.set(null);
    this.jobStatus.set(null);
    this.connectionStatus.set(null);
    this.created.set(0);
    this.total.set(0);
    this.lastRevision = -1;
    this.resultRequested = false;
    sessionStorage.removeItem(this.storageKey());
    this.isSeeding.set(true);
    this.dialogRef.disableClose = true;
    this.errorMessage.set(null);
    this.result.set(null);

    let targetDbs: number[] | undefined;
    let isRandom = false;

    if (this.dbMode() === 'current') {
      targetDbs = [this.data.database];
    } else if (this.dbMode() === 'specific') {
      targetDbs = this.selectedDbs();
    } else {
      isRandom = true;
    }

    const options: SeedSampleDataOptions = {
      keyCount: this.keyCount(),
      targetDatabases: targetDbs,
      distributeRandomlyAcrossDatabases: isRandom,
      randomDbCount: isRandom ? this.randomDbCount() : undefined,
      keyComplexity: this.keyComplexity(),
      payloadComplexity: this.payloadComplexity(),
      dataTypes: this.selectedTypes(),
      ttlStrategy: this.ttlStrategy(),
      keyPrefix: this.seedModel().keyPrefix.trim() || 'demo'
    };

    this.jobs
      .start('seed-data', this.data.connectionId, options)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: job => {
          this.dialogRef.disableClose = false;
          this.jobId.set(job.jobId);
          sessionStorage.setItem(this.storageKey(), job.jobId);
          this.watch(job.jobId);
        },
        error: err => {
          this.dialogRef.disableClose = false;
          this.isSeeding.set(false);
          this.errorMessage.set(err.error?.message || err.message || 'Failed to seed sample data');
        }
      });
  }

  stopSeed(): void {
    const id = this.jobId();
    if (!id) return;
    this.stopping.set(true);
    this.jobs.cancel(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => { this.stopping.set(false); this.errorMessage.set('Could not request cancellation.'); }
    });
  }

  refreshStatus(): void {
    const id = this.jobId();
    if (id) this.jobs.status(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: job => this.applyJob(job),
      error: () => { this.isSeeding.set(false); this.errorMessage.set('Job status is unavailable or has expired.'); }
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
    this.isSeeding.set(['Queued', 'Running', 'Cancelling'].includes(job.status));
    this.stopping.set(job.status === 'Cancelling');
    if (job.metrics) {
      const metrics = job.metrics as { created: number; total: number };
      this.created.set(metrics.created);
      this.total.set(metrics.total);
    }
    if (job.status === 'Failed') this.errorMessage.set(job.error || 'Seeding failed.');
    if (job.status === 'Cancelled' && !job.hasResult) this.errorMessage.set('Seeding was cancelled before a result was available.');
    if (job.hasResult && !this.resultRequested) {
      this.resultRequested = true;
      this.jobs.result<SeedDataResult>(job.jobId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => { if (this.jobId() === job.jobId) this.result.set(result); },
        error: () => { if (this.jobId() === job.jobId) { this.resultRequested = false; this.errorMessage.set('Could not retrieve the job result.'); } }
      });
    }
  }

  private storageKey(): string { return `redisgui:seed:${this.data.connectionId}:${this.data.database}`; }
}
