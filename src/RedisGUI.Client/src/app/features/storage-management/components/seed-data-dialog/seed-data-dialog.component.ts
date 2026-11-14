import { SeedResult } from './seed-result';
import { Component, inject, signal, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  RedisCliService,
  SeedSampleDataOptions,
  SeedDataResult
} from '../../../../core/services/redis-cli.service';
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
  private readonly cliService = inject(RedisCliService);
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
    this.isSeeding.set(true);
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

    this.cliService
      .seedSampleData(this.data.connectionId, options)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.isSeeding.set(false);
          this.result.set(res);
        },
        error: err => {
          this.isSeeding.set(false);
          this.errorMessage.set(err.error?.message || err.message || 'Failed to seed sample data');
        }
      });
  }
}
