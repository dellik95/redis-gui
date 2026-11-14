import { KeySearchControls } from './key-search-controls';
import { KeyTypeFilter } from './key-type-filter';
import { TypeBadgeComponent } from '../storage-value-editor/components/type-badge.component';
import {
  Component,
  signal,
  model,
  viewChild,
  input,
  inject,
  untracked,
  effect,
  computed,
  ChangeDetectionStrategy,
  DestroyRef,
  output
} from '@angular/core';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTable, MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';

import { ScrollingModule } from '@angular/cdk/scrolling';
import { SelectionModel } from '@angular/cdk/collections';

import { BulkActionsToolbarComponent } from '../bulk-actions-toolbar/bulk-actions-toolbar.component';
import { TreeViewListComponent } from '../../../../shared/components/tree-view-list/tree-view-list.component';
import { ValueEditorOutput } from '../storage-value-editor/storage-value-editor.component';
import { ConfirmDialogComponent, PromptDialogComponent, EmptyStateComponent } from '../../../../shared/components';
import type { RedisTypeCounts } from './key-type-filter';
import { FormatBytesPipe } from '../../../../shared/pipes/format-bytes.pipe';
import { filter } from 'rxjs/operators';

import { StorageManagementStore } from '../../stores/storage-management.store';
import { RedisKeysStore } from '../../../../core/stores/redis-keys.store';
import { RedisOperationsStore } from '../../../../core/stores/redis-operations.store';
import {
  RedisDataType,
  RedisKeyInfo,
  SelectedRedisKey,
  toRedisDataType
} from '../../../../models/redis-keys.types';

/**
 * Storage Values List Component (Angular 22)
 * Main component for exploring, filtering, and managing Redis keys
 */
@Component({
  selector: 'app-storage-values-list',
  host: { '(document:keydown)': 'onWindowKeyDown($event)' },
  standalone: true,
  templateUrl: './storage-values-list.component.html',
  styleUrl: './storage-values-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    KeySearchControls,
    KeyTypeFilter,
    MatTableModule,
    ScrollingModule,
    MatIconModule,
    MatButtonModule,
    TreeViewListComponent,
    BulkActionsToolbarComponent,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatPaginatorModule,
    MatCheckboxModule,
    FormatBytesPipe,
    TypeBadgeComponent,
    EmptyStateComponent
  ]
})
export class StorageValuesListComponent {
  // 1. Injected dependencies
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly storageStore = inject(StorageManagementStore);
  private readonly redisKeysStore = inject(RedisKeysStore);
  private readonly redisOperationsStore = inject(RedisOperationsStore);
  private readonly destroyRef = inject(DestroyRef);

  // 2. Inputs & Outputs
  readonly connectionId = input.required<string>();
  readonly activeKey = input<string | null>(null);
  readonly keySelected = output<SelectedRedisKey>();

  // 3. View queries
  readonly table = viewChild(MatTable);

  // 4. Model signals (Angular 22 two-way signal bindings)
  readonly viewMode = model<'table' | 'tree'>('table');
  readonly keyDelimiter = model<string>(':');
  readonly searchPattern = model<string>('*');
  readonly typeFilter = model<string>('');
  readonly sortField = model<string>('key');
  readonly sortDir = model<'asc' | 'desc'>('asc');

  // 5. State signals
  readonly isLoading = computed(() => this.redisKeysStore.isLoading() && this.keys().length === 0);
  readonly chunkLoading = signal<boolean>(false);
  readonly pageSize = signal<number>(50);
  readonly pageNumber = signal<number>(1);
  readonly selectedDb = signal<number>(0);
  readonly fetchTimestamp = signal<number>(Date.now());
  readonly currentTimestamp = signal<number>(Date.now());

  readonly availableDatabases = computed<{ index: number; count: number }[]>(() => {
    const keyspace = this.redisKeysStore.keyspace();
    const result: { index: number; count: number }[] = [];
    for (let i = 0; i <= 15; i++) {
      result.push({
        index: i,
        count: keyspace[i] ?? 0
      });
    }
    return result;
  });

  // 6. Data source and selection
  readonly keys = computed(() =>
    this.redisKeysStore.keys().map(key => ({ ...key, type: toRedisDataType(key.type) }))
  );
  readonly selection = new SelectionModel<string>(true, []);

  // 7. Configuration
  readonly displayedColumns: readonly string[] = [
    'select',
    'type',
    'key',
    'ttl',
    'size',
    'actions'
  ] as const;

  // 8. Type counting for filter badges

  readonly typeCounts = computed<RedisTypeCounts>(() => {
    const data = this.keys();
    const counts: RedisTypeCounts = {
      all: data.length,
      string: 0,
      list: 0,
      set: 0,
      hash: 0,
      sortedset: 0,
      stream: 0
    };
    data.forEach(item => {
      const t = item.type.toLowerCase();
      switch (t) {
        case 'string':
          counts.string++;
          break;
        case 'list':
          counts.list++;
          break;
        case 'set':
          counts.set++;
          break;
        case 'hash':
          counts.hash++;
          break;
        case 'sortedset':
        case 'zset':
          counts.sortedset++;
          break;
        case 'stream':
          counts.stream++;
          break;
      }
    });
    return counts;
  });

  // 9. Computed filtered data and total count
  readonly filteredData = computed<RedisKeyInfo[]>(() => {
    let data = this.keys();
    const filterVal = this.typeFilter();

    if (filterVal) {
      const target = filterVal.toLowerCase();
      data = data.filter(d => {
        const itemType = d.type.toLowerCase();
        if (target === 'sortedset' || target === 'zset') {
          return itemType === 'sortedset' || itemType === 'zset';
        }
        return itemType === target;
      });
    }

    const field = this.sortField();
    const dir = this.sortDir();

    return [...data].sort((a, b) => {
      let vA: string | number;
      let vB: string | number;

      switch (field) {
        case 'key':
          vA = a.key;
          vB = b.key;
          break;
        case 'type':
          vA = a.type;
          vB = b.type;
          break;
        case 'ttl':
          vA = Number(a.ttl ?? -1);
          vB = Number(b.ttl ?? -1);
          break;
        case 'size':
          vA = Number(a.size ?? 0);
          vB = Number(b.size ?? 0);
          break;
        default:
          vA = a.key;
          vB = b.key;
      }

      if (vA < vB) return dir === 'asc' ? -1 : 1;
      if (vA > vB) return dir === 'asc' ? 1 : -1;
      return 0;
    });
  });

  readonly totalCount = computed<number>(() => this.filteredData().length);

  readonly trackByKey = (_index: number, item: RedisKeyInfo) => item.key;

  constructor() {
    effect(() => {
      this.connectionId();
      untracked(() => this.loadKeys());
    });

    // Live countdown interval ticker
    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentTimestamp.set(Date.now());
      });
  }

  onScroll(index: number): void {
    const dataLength = this.keys().length;
    if (
      dataLength >= this.pageSize() * this.pageNumber() &&
      index > dataLength - 20 &&
      !this.chunkLoading()
    ) {
      this.chunkLoading.set(true);
      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          this.chunkLoading.set(false);
        });
      });
    }
  }

  setTypeFilter(type: string): void {
    this.typeFilter.set(this.typeFilter() === type ? '' : type);
  }

  clearSearch(): void {
    this.searchPattern.set('*');
    this.loadKeys();
  }

  toggleViewMode(mode: string): void {
    this.viewMode.set(mode as 'table' | 'tree');
  }

  changeKeyDelimiter(): void {
    const delimiters = [':', '.', '/', '-', '_'];
    const current = this.keyDelimiter();
    const currentIndex = delimiters.indexOf(current);
    const nextIndex = (currentIndex + 1) % delimiters.length;
    const nextDelimiter = delimiters[nextIndex];
    if (nextDelimiter) {
      this.keyDelimiter.set(nextDelimiter);
    }
  }

  toggleSort(field: string): void {
    if (this.sortField() === field) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDir.set('asc');
    }
  }

  onSearchPatternChange(pattern: string): void {
    this.searchPattern.set(pattern || '*');
    this.loadKeys();
  }

  loadKeys(): void {
    this.fetchTimestamp.set(Date.now());
    this.storageStore.loadKeys(this.searchPattern(), this.keyDelimiter(), this.selectedDb());
    this.redisKeysStore.loadKeyspace({ connectionId: this.connectionId() });
  }

  onDatabaseChange(db: number): void {
    this.selectedDb.set(db);
    this.redisKeysStore.setActiveDatabase(db);
    this.loadKeys();
  }

  getLiveTtl(originalTtl: number | null): number | null {
    if (originalTtl === null || originalTtl <= 0) return originalTtl;
    const elapsedSec = Math.floor((this.currentTimestamp() - this.fetchTimestamp()) / 1000);
    return Math.max(0, originalTtl - elapsedSec);
  }

  async openCommandPalette(): Promise<void> {
    const { CommandPaletteComponent } =
      await import('../../../../shared/components/command-palette/command-palette.component');
    if (this.dialog.getDialogById('command-palette')) return;
    this.dialog.open(CommandPaletteComponent, {
      id: 'command-palette',
      width: '680px',
      maxWidth: '94vw',
      panelClass: 'command-palette-dialog-panel',
      enterAnimationDuration: '0ms',
      exitAnimationDuration: '0ms',
      data: {
        connectionId: this.connectionId(),
        onAddNewKey: () => this.addNewKey(),
        onSeedData: () => this.seedDemoData(),
        onOpenSlowlog: () => this.openSlowlog(),
        onOpenLoadTest: () => this.openLoadTest(),
        onExport: () => this.exportData('json'),
        onImport: () => this.importData(),
        onRefresh: () => this.loadKeys(),
        onSelectKey: (k: string) => this.editKey(k),
        onSwitchDb: (db: number) => this.onDatabaseChange(db)
      }
    });
  }

  onWindowKeyDown(e: KeyboardEvent): void {
    if (!e.defaultPrevented && (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      this.openCommandPalette();
    }
  }

  async addNewKey(folderPrefix?: string): Promise<void> {
    const { AddKeyComponent } = await import('../add-key/add-key.component');
    const dialogRef = this.dialog.open(AddKeyComponent, {
      width: '900px',
      maxWidth: '92vw',
      maxHeight: '90vh',
      panelClass: 'custom-key-dialog',
      data: {
        connectionId: this.connectionId(),
        folderPrefix: folderPrefix || ''
      }
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result: ValueEditorOutput | undefined) => {
        if (result) {
          this.redisKeysStore.addValue({
            connectionId: this.connectionId(),
            key: result.key,
            type: result.type,
            value: result.value,
            database: this.selectedDb(),
            ...(result.ttl !== undefined ? { ttl: result.ttl } : {}),
            onSuccess: () => {
              this.redisKeysStore.clearCache();
              this.loadKeys();
              this.snackBar.open(`Key "${result.key}" created successfully`, 'Close', {
                duration: 3000
              });
            }
          });
        }
      });
  }

  deleteKey(key: string): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete Key',
          message: `Are you sure you want to delete the key "${key}"?`,
          confirmText: 'Delete',
          cancelText: 'Cancel',
          confirmColor: 'warn'
        }
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        this.selection.deselect(key);
        this.redisKeysStore.deleteKey({
          connectionId: this.connectionId(),
          key,
          database: this.selectedDb(),
          onSuccess: () => {
            this.redisKeysStore.clearCache();
            this.loadKeys();
            this.snackBar.open(`Key "${key}" deleted`, 'Close', { duration: 3000 });
          }
        });
      });
  }

  deleteFolder(event: { path: string; name: string; keyCount: number; keys: string[] }): void {
    const delim = this.keyDelimiter() || ':';
    const pattern = event.path.endsWith(delim) ? `${event.path}*` : `${event.path}${delim}*`;

    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: `Delete Folder "${event.name}"`,
          message: `Are you sure you want to delete folder "${event.path}" and all ${event.keyCount} key(s) under it? This will delete all keys matching "${pattern}". This action cannot be undone.`,
          confirmText: `Delete ${event.keyCount} Key(s)`,
          cancelText: 'Cancel',
          confirmColor: 'warn'
        }
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        if (event.keys.length > 0) {
          this.redisOperationsStore.deleteMultipleKeys({
            connectionId: this.connectionId(),
            keys: event.keys,
            database: this.selectedDb(),
            onSuccess: () => {
              this.selection.clear();
              this.redisKeysStore.clearCache();
              this.loadKeys();
              this.snackBar.open(
                `Folder "${event.name}" and ${event.keys.length} key(s) deleted`,
                'Close',
                { duration: 3500 }
              );
            }
          });
        } else {
          this.redisOperationsStore.deleteKeysByPattern({
            connectionId: this.connectionId(),
            pattern,
            database: this.selectedDb(),
            onSuccess: () => {
              this.selection.clear();
              this.redisKeysStore.clearCache();
              this.loadKeys();
              this.snackBar.open(`Folder "${event.name}" keys deleted`, 'Close', {
                duration: 3500
              });
            }
          });
        }
      });
  }

  copyKey(key: string, event?: Event): void {
    if (event) event.stopPropagation();
    navigator.clipboard.writeText(key);
    this.snackBar.open(`Copied "${key}" to clipboard`, 'Close', { duration: 2000 });
  }

  editKey(key: string): void {
    const item = this.keys().find(row => row.key === key);
    this.keySelected.emit({ key, type: item?.type ?? RedisDataType.String, ttl: item?.ttl ?? -1 });
  }

  onTreeKeySelected(event: SelectedRedisKey): void {
    this.keySelected.emit(event);
  }

  formatTtl(seconds: number): string {
    if (seconds <= 0) return 'No expiry';
    if (seconds < 60) return `${seconds}s`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`;
    if (seconds < 86400)
      return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
    return `${Math.floor(seconds / 86400)}d ${Math.floor((seconds % 86400) / 3600)}h`;
  }

  onPageChange(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.pageNumber.set(event.pageIndex + 1);
    this.loadKeys();
  }

  isAllSelected(): boolean {
    const numSelected = this.selection.selected.length;
    const currentPageData = this.filteredData();
    return numSelected === currentPageData.length && currentPageData.length > 0;
  }

  masterToggle(): void {
    if (this.isAllSelected()) {
      this.selection.clear();
    } else {
      const currentPageData = this.filteredData();
      currentPageData.forEach((row: RedisKeyInfo) => this.selection.select(row.key));
    }
  }

  deleteSelected(): void {
    const selectedKeys = [...this.selection.selected];
    if (selectedKeys.length === 0) {
      this.snackBar.open('No keys selected', 'Close', { duration: 3000 });
      return;
    }

    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete Selected Keys',
          message: `Are you sure you want to delete ${selectedKeys.length} selected key(s)?`,
          confirmText: 'Delete',
          cancelText: 'Cancel',
          confirmColor: 'warn'
        }
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        this.redisOperationsStore.deleteMultipleKeys({
          connectionId: this.connectionId(),
          keys: selectedKeys,
          database: this.selectedDb(),
          onSuccess: () => {
            this.selection.clear();
            this.redisKeysStore.clearCache();
            this.loadKeys();
            this.snackBar.open(`Deleted ${selectedKeys.length} key(s)`, 'Close', {
              duration: 4000
            });
          }
        });
      });
  }

  deleteByPattern(): void {
    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: 'Delete by Pattern',
          message: 'Enter pattern to delete (e.g., user:*, session:*):',
          placeholder: 'Pattern',
          defaultValue: this.searchPattern(),
          required: true
        }
      })
      .afterClosed()
      .pipe(filter((pattern): pattern is string => pattern !== null && pattern.trim().length > 0))
      .subscribe(pattern => {
        this.dialog
          .open(ConfirmDialogComponent, {
            data: {
              title: 'Confirm Delete',
              message: `Are you sure you want to delete all keys matching "${pattern}"?`,
              confirmText: 'Delete',
              cancelText: 'Cancel',
              confirmColor: 'warn'
            }
          })
          .afterClosed()
          .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
          .subscribe(() => {
            this.redisOperationsStore.deleteKeysByPattern({
              connectionId: this.connectionId(),
              pattern,
              database: this.selectedDb(),
              onSuccess: () => {
                this.selection.clear();
                this.redisKeysStore.clearCache();
                this.loadKeys();
                this.snackBar.open(`Deleted keys matching "${pattern}"`, 'Close', {
                  duration: 4000
                });
              }
            });
          });
      });
  }

  async exportData(format: 'json' | 'csv'): Promise<void> {
    const { ExportDialogComponent } = await import('../export/export-dialog.component');
    const ref = this.dialog.open(ExportDialogComponent, {
      data: { connectionId: this.connectionId(), format }
    });
    ref.afterClosed().subscribe(success => {
      if (success) this.snackBar.open('Export completed', 'Close', { duration: 3000 });
    });
  }

  async importData(): Promise<void> {
    const { ImportDialogComponent } = await import('../import/import-dialog.component');
    const dialogRef = this.dialog.open(ImportDialogComponent, {
      data: { connectionId: this.connectionId() }
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(success => {
        if (success) {
          this.redisKeysStore.clearCache();
          this.snackBar.open('Data imported successfully', 'Close', { duration: 3000 });
          this.loadKeys();
        }
      });
  }

  renameKey(oldKey: string): void {
    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: 'Rename Key',
          message: `Enter new name for key "${oldKey}":`,
          placeholder: 'New key name',
          defaultValue: oldKey,
          required: true
        }
      })
      .afterClosed()
      .pipe(
        filter(
          (newKey): newKey is string =>
            newKey !== null && newKey.trim().length > 0 && newKey !== oldKey
        )
      )
      .subscribe(newKey => {
        this.redisKeysStore.renameKey({
          connectionId: this.connectionId(),
          oldKey,
          newKey: newKey.trim(),
          database: this.selectedDb(),
          onSuccess: () => {
            this.snackBar.open(`Renamed "${oldKey}" to "${newKey}"`, 'Close', { duration: 3000 });
            this.loadKeys();
          }
        });
      });
  }

  duplicateKey(sourceKey: string): void {
    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: 'Duplicate Key',
          message: 'Enter name for cloned key:',
          placeholder: 'Target key name',
          defaultValue: `${sourceKey}_copy`,
          required: true
        }
      })
      .afterClosed()
      .pipe(
        filter(
          (targetKey): targetKey is string => targetKey !== null && targetKey.trim().length > 0
        )
      )
      .subscribe(targetKey => {
        this.redisKeysStore.duplicateKey({
          connectionId: this.connectionId(),
          sourceKey,
          targetKey: targetKey.trim(),
          database: this.selectedDb(),
          onSuccess: () => {
            this.snackBar.open(`Cloned "${sourceKey}" as "${targetKey}"`, 'Close', {
              duration: 3000
            });
            this.loadKeys();
          }
        });
      });
  }

  openBulkTtl(): void {
    const selected = this.selection.selected;
    if (selected.length === 0) return;

    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: `Set TTL for ${selected.length} keys`,
          message:
            'Enter TTL in seconds (e.g. 300 for 5m, 3600 for 1h, or 0 to PERSIST indefinitely):',
          placeholder: 'TTL in seconds (0 = persist)',
          defaultValue: '300',
          required: true
        }
      })
      .afterClosed()
      .pipe(filter((val): val is string => val !== null && val.trim().length > 0))
      .subscribe(val => {
        const seconds = parseInt(val.trim(), 10);
        const ttlSeconds = isNaN(seconds) || seconds <= 0 ? null : seconds;

        this.redisKeysStore.bulkUpdateTtl({
          connectionId: this.connectionId(),
          keys: selected,
          ttlSeconds,
          database: this.selectedDb(),
          onSuccess: count => {
            const label = ttlSeconds
              ? `TTL set to ${ttlSeconds}s for ${count} keys`
              : `Persisted ${count} keys indefinitely`;
            this.snackBar.open(label, 'Close', { duration: 3000 });
            this.selection.clear();
            this.loadKeys();
          }
        });
      });
  }

  async seedDemoData(): Promise<void> {
    const { SeedDataDialogComponent } =
      await import('../seed-data-dialog/seed-data-dialog.component');
    this.dialog
      .open(SeedDataDialogComponent, {
        width: '740px',
        maxWidth: '92vw',
        data: {
          connectionId: this.connectionId(),
          database: this.selectedDb()
        }
      })
      .afterClosed()
      .pipe(filter((seeded): seeded is boolean => seeded === true))
      .subscribe(() => {
        this.snackBar.open('Demo data successfully seeded!', 'Close', { duration: 3000 });
        this.loadKeys();
      });
  }

  async openSlowlog(): Promise<void> {
    const { SlowlogDialogComponent } = await import('../slowlog-dialog/slowlog-dialog.component');
    this.dialog.open(SlowlogDialogComponent, {
      width: '760px',
      maxWidth: '92vw',
      data: { connectionId: this.connectionId() }
    });
  }

  async openLoadTest(): Promise<void> {
    const { LoadTestDialogComponent } =
      await import('../load-test-dialog/load-test-dialog.component');
    this.dialog.open(LoadTestDialogComponent, {
      width: '680px',
      maxWidth: '94vw',
      data: {
        connectionId: this.connectionId(),
        database: this.selectedDb()
      }
    });
  }
}
