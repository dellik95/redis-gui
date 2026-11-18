import { SeedResult } from './seed-result';
import { Component, computed, inject, signal, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SeedSampleDataOptions, SeedDataResult } from '../../../../core/services/redis-cli.service';
import { BackgroundJobsStore } from '../../../../core/stores/background-jobs.store';
import { SeedDataJobProgress, isJobActive } from '../../../../core/models/background-jobs.types';
import { NumberPresetOption, NumberPresetPickerComponent } from '../../../../shared/components';
import { UiButtonComponent } from '../../../../shared/ui';

const KEY_COUNT_PRESETS: readonly NumberPresetOption[] = [
  { value: 25, label: '25' },
  { value: 50, label: '50' },
  { value: 100, label: '100' },
  { value: 250, label: '250' },
  { value: 500, label: '500' },
  { value: 1000, label: '1,000' },
  { value: 5000, label: '5,000' },
  { value: 10000, label: '10,000' }
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
  jobId?: string | undefined;
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
    NumberPresetPickerComponent,
    UiButtonComponent
  ],
  templateUrl: './seed-data-dialog.component.html',
  styleUrl: './seed-data-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SeedDataDialogComponent {
  readonly data: SeedDataDialogData = inject(MAT_DIALOG_DATA);
  private readonly jobsStore = inject(BackgroundJobsStore);
  readonly dialogRef = inject(MatDialogRef<SeedDataDialogComponent>);
  private readonly destroyRef = inject(DestroyRef);

  readonly availableDbs = Array.from({ length: 16 }, (_, i) => i);

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

  readonly jobId = signal<string | null>(
    this.data.jobId ??
      this.jobsStore
        .activeJobs()
        .find(e => e.job.kind === 'seed-data' && e.job.connectionId === this.data.connectionId)?.job
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
  readonly isSeeding = computed(() => {
    if (this.starting()) return true;
    const status = this.entry()?.job.status;
    return status ? isJobActive(status) : false;
  });
  readonly stopping = computed(() => this.entry()?.job.status === 'Cancelling');
  readonly result = computed<SeedDataResult | null>(
    () => (this.entry()?.resultData as SeedDataResult | null) ?? null
  );
  readonly created = computed(() => {
    const metrics = this.entry()?.job.metrics as SeedDataJobProgress | null;
    return metrics?.created ?? 0;
  });
  readonly total = computed(() => {
    const metrics = this.entry()?.job.metrics as SeedDataJobProgress | null;
    return metrics?.total ?? 0;
  });
  readonly errorMessage = computed(() => {
    if (this.localError()) return this.localError();
    const current = this.entry();
    if (!current) return null;
    if (current.expiredReason) return current.expiredReason;
    if (current.job.status === 'Cancelled' && !current.job.hasResult) {
      return 'Seeding was cancelled before a result was available.';
    }
    return current.job.error ?? current.resultError ?? null;
  });
  readonly connectionStatus = computed(() =>
    this.jobsStore.liveConnected() ? null : (this.jobsStore.connectionNotice() || 'Reconnecting live updates…')
  );

  constructor() {
    const initialId = this.jobId();
    if (initialId && !this.entry()) {
      this.jobsStore.refreshJob(initialId);
    }
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
    this.jobId.set(null);
    this.localError.set(null);
    this.starting.set(true);
    this.dialogRef.disableClose = true;

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

    this.jobsStore
      .startJob('seed-data', this.data.connectionId, options, {
        database: this.data.database,
        markSeen: true
      })
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
          this.localError.set(err?.error?.message || err?.message || 'Failed to seed sample data');
        }
      });
  }

  stopSeed(): void {
    const id = this.jobId();
    if (!id) return;
    this.jobsStore
      .cancelJob(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.localError.set('Could not request cancellation.')
      });
  }
}
