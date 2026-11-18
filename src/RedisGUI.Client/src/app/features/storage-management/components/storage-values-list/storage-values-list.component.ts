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
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_ROUTES } from '../../../../core/constants/api-routes.constants';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';

import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';
import { SelectionModel } from '@angular/cdk/collections';

import { BulkActionsToolbarComponent } from '../bulk-actions-toolbar/bulk-actions-toolbar.component';
import { TreeViewListComponent } from '../../../../shared/components/tree-view-list/tree-view-list.component';
import { ValueEditorOutput } from '../storage-value-editor/storage-value-editor.component';
import {
  ConfirmDialogComponent,
  PromptDialogComponent,
  EmptyStateComponent
} from '../../../../shared/components';
import type { RedisTypeCounts } from './key-type-filter';
import { filter } from 'rxjs/operators';

import { StorageManagementStore } from '../../stores/storage-management.store';
import { ConnectionsStore } from '../../../../core/stores/connections.store';
import {
  RedisKeysStore,
  redisKeyIdentity
} from '../../../../core/stores/redis-keys.store';
import {
  RedisDataType,
  RedisKeyInfo,
  SelectedRedisKey,
  toRedisDataType
} from '../../../../models/redis-keys.types';

/**
 * Storage Values List Component (Angular 22)
 * Main component for exploring, filtering, and managing Redis keys with CDK virtual scrolling
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
    ScrollingModule,
    MatIconModule,
    MatButtonModule,
    TreeViewListComponent,
    BulkActionsToolbarComponent,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatCheckboxModule,
    TypeBadgeComponent,
    EmptyStateComponent
  ]
})
export class StorageValuesListComponent {
  private readonly dialog = inject(MatDialog);
  private readonly http = inject(HttpClient);
  private readonly snackBar = inject(MatSnackBar);
  private readonly storageStore = inject(StorageManagementStore);
  private readonly redisKeysStore = inject(RedisKeysStore);
  private readonly connectionsStore = inject(ConnectionsStore);
  private readonly destroyRef = inject(DestroyRef);

  readonly connectionId = input.required<string>();
  readonly activeKey = input<string | null>(null);
  readonly activeKeyBase64 = input<string | null>(null);
  readonly keySelected = output<SelectedRedisKey>();

  readonly scrollViewport = viewChild(CdkVirtualScrollViewport);

  readonly viewMode = model<'table' | 'tree'>('table');
  readonly keyDelimiter = model<string>(':');
  readonly searchPattern = model<string>('*');
  readonly typeFilter = model<string>('');
  readonly sortField = model<string>('key');
  readonly sortDir = model<'asc' | 'desc'>('asc');

  readonly isLoading = computed(() => this.redisKeysStore.isLoading() && this.keys().length === 0);
  readonly scanLoading = this.redisKeysStore.isLoading;
  readonly nextCursor = this.redisKeysStore.nextCursor;
  readonly scanError = this.redisKeysStore.error;
  readonly retentionLimitReached = this.redisKeysStore.retentionLimitReached;
  readonly isCluster = computed(() => {
    const conn =
      this.connectionsStore.getConnectionById(this.connectionId()) ??
      this.connectionsStore.selectedConnection();
    return conn?.serverMode === 'Cluster' || this.redisKeysStore.serverMode() === 'Cluster';
  });

  readonly selectedDb = signal<number>(0);
  readonly fetchTimestamp = signal<number>(Date.now());
  readonly currentTimestamp = signal<number>(Date.now());

  readonly availableDatabases = computed<{ index: number; count: number }[]>(() => {
    if (this.isCluster()) {
      const keyspace = this.redisKeysStore.keyspace();
      return [{ index: 0, count: keyspace[0] ?? 0 }];
    }
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

  readonly keys = computed(() =>
    this.redisKeysStore.keys().map(key => ({ ...key, type: toRedisDataType(key.type) }))
  );

  /** Keyed by canonical identity (`b64:...` or `txt:...`) so binary collisions cannot happen. */
  readonly selection = new SelectionModel<string>(true, []);
  readonly selectedCount = signal<number>(0);

  private readonly keysByIdentity = computed(() => {
    const map = new Map<string, RedisKeyInfo>();
    for (const item of this.keys()) {
      map.set(redisKeyIdentity(item), item);
    }
    return map;
  });

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

  readonly trackByIdentity = (_index: number, item: RedisKeyInfo) => redisKeyIdentity(item);

  constructor() {
    effect(() => {
      const connId = this.connectionId();
      untracked(() => {
        const conn =
          this.connectionsStore.getConnectionById(connId) ??
          this.connectionsStore.selectedConnection();
        const initialDb = conn?.serverMode === 'Cluster' ? 0 : (conn?.database ?? 0);
        this.selectedDb.set(initialDb);
        this.redisKeysStore.setActiveDatabase(initialDb);
        this.loadKeys();
      });
    });

    effect(() => {
      if (this.isCluster() && this.selectedDb() !== 0) {
        untracked(() => {
          this.selectedDb.set(0);
          this.redisKeysStore.setActiveDatabase(0);
        });
      }
    });

    effect(() => {
      const visibleCount = this.filteredData().length;
      const cursor = this.nextCursor();
      const loading = this.scanLoading();
      const limitReached = this.retentionLimitReached();
      const err = this.scanError();
      if (visibleCount < 25 && cursor && !loading && !limitReached && !err) {
        untracked(() => this.loadMore());
      }
    });

    this.selection.changed.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.selectedCount.set(this.selection.selected.length);
    });

    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentTimestamp.set(Date.now());
      });
  }

  onScrolledIndexChange(firstVisibleIndex: number): void {
    const totalLoaded = this.filteredData().length;
    if (
      totalLoaded > 0 &&
      firstVisibleIndex + 25 >= totalLoaded &&
      this.nextCursor() &&
      !this.scanLoading() &&
      !this.retentionLimitReached()
    ) {
      this.loadMore();
    }
  }

  rowIdentity(item: RedisKeyInfo): string {
    return redisKeyIdentity(item);
  }

  isRowActive(item: RedisKeyInfo): boolean {
    const activeB64 = this.activeKeyBase64();
    if (activeB64 && item.keyBase64) {
      return activeB64 === item.keyBase64;
    }
    return this.activeKey() === item.key;
  }

  setTypeFilter(type: string): void {
    this.typeFilter.set(this.typeFilter() === type ? '' : type);
  }

  clearSearch(): void {
    this.searchPattern.set('*');
    this.loadKeys();
  }

  toggleViewMode(mode: string): void {
    const nextMode = mode as 'table' | 'tree';
    this.viewMode.set(nextMode);
    if (nextMode === 'table') {
      setTimeout(() => this.scrollViewport()?.checkViewportSize(), 0);
    }
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
    const connId = this.connectionId();
    if (this.storageStore.connectionId() !== connId) {
      this.storageStore.initialize(connId);
    }
    this.fetchTimestamp.set(Date.now());
    this.selection.clear();
    this.storageStore.loadKeys(this.searchPattern(), this.keyDelimiter(), this.selectedDb());
    this.redisKeysStore.loadKeyspace({ connectionId: connId });
  }

  loadMore(): void {
    if (this.scanLoading() || !this.nextCursor() || this.retentionLimitReached()) return;
    this.redisKeysStore.loadKeys({
      connectionId: this.connectionId(),
      pattern: this.searchPattern(),
      database: this.selectedDb(),
      append: true,
      autoContinueEmptyPages: true
    });
  }

  onDatabaseChange(db: number): void {
    if (this.isCluster() && db !== 0) {
      this.snackBar.open('Redis Cluster supports database 0 only.', 'Close', { duration: 3000 });
      return;
    }
    this.selectedDb.set(db);
    this.redisKeysStore.setActiveDatabase(db);
    this.loadKeys();
  }

  getLiveTtl(originalTtl: number | null, fetchedAt?: number): number | null {
    if (originalTtl === null || originalTtl <= 0) return originalTtl;
    const elapsedSec = Math.floor(
      (this.currentTimestamp() - (fetchedAt ?? this.fetchTimestamp())) / 1000
    );
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

  deleteKey(target: string | RedisKeyInfo): void {
    const item = this.resolveKeyItem(target);
    const key = item?.key ?? (typeof target === 'string' ? target : target.key);
    const keyBase64 = item?.keyBase64 ?? null;

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
        this.selection.deselect(redisKeyIdentity({ key, keyBase64 }));
        this.redisKeysStore.deleteKey({
          connectionId: this.connectionId(),
          key,
          keyBase64,
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
    const database = this.selectedDb();

    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: `Delete Folder "${event.name}"`,
          message:
            event.keys.length > 0
              ? `Delete the ${event.keys.length} loaded key(s) in "${event.path}"? Keys not loaded in the browser will not be deleted. This action cannot be undone.`
              : `Delete all keys matching "${pattern}", including keys not loaded in the browser? This action cannot be undone.`,
          confirmText:
            event.keys.length > 0 ? `Delete ${event.keys.length} Key(s)` : 'Delete matching keys',
          cancelText: 'Cancel',
          confirmColor: 'warn'
        }
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        if (event.keys.length > 0) {
          const items = event.keys.map(k => this.resolveKeyItem(k) ?? { key: k, type: 'string', ttl: null, size: 0, memoryUsage: null });
          void this.runSelectedBulk('delete', items, database);
        } else {
          void this.runPatternBulk(pattern, database);
        }
      });
  }

  copyKey(target: string | RedisKeyInfo, event?: Event): void {
    if (event) event.stopPropagation();
    const item = this.resolveKeyItem(target);
    if (item?.isBinary && item.keyBase64) {
      navigator.clipboard.writeText(item.keyBase64);
      this.snackBar.open('Binary key copied as Base64', 'Close', { duration: 2500 });
      return;
    }
    const text = typeof target === 'string' ? target : target.key;
    navigator.clipboard.writeText(text);
    this.snackBar.open(`Copied "${text}" to clipboard`, 'Close', { duration: 2000 });
  }

  editKey(target: string | RedisKeyInfo): void {
    const item = this.resolveKeyItem(target);
    const key = item?.key ?? (typeof target === 'string' ? target : target.key);
    this.keySelected.emit({
      key,
      keyBase64: item?.keyBase64 ?? null,
      isBinary: item?.isBinary ?? false,
      type: toRedisDataType(item?.type ?? 'string'),
      ttl: item?.ttl ?? -1
    });
  }

  onTreeKeySelected(event: SelectedRedisKey): void {
    const item = this.resolveKeyItem(event.key);
    this.keySelected.emit({
      ...event,
      keyBase64: event.keyBase64 ?? item?.keyBase64 ?? null,
      isBinary: event.isBinary ?? item?.isBinary ?? false
    });
  }

  formatTtl(seconds: number): string {
    if (seconds <= 0) return 'No expiry';
    if (seconds < 60) return `${seconds}s`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`;
    if (seconds < 86400)
      return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
    return `${Math.floor(seconds / 86400)}d ${Math.floor((seconds % 86400) / 3600)}h`;
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
      currentPageData.forEach(row => this.selection.select(redisKeyIdentity(row)));
    }
  }

  deleteSelected(): void {
    const items = this.getSelectedItems();
    const database = this.selectedDb();
    if (items.length === 0) {
      this.snackBar.open('No keys selected', 'Close', { duration: 3000 });
      return;
    }

    const preview = items.slice(0, 5).map(i => i.key).join(', ');
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete Selected Keys',
          message: `Delete ${items.length} selected key(s) from DB ${database}? Examples: ${preview}${items.length > 5 ? ', …' : ''}. Keys may change before deletion.`,
          confirmText: 'Delete',
          cancelText: 'Cancel',
          confirmColor: 'warn'
        }
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        void this.runSelectedBulk('delete', items, database);
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
        const database = this.selectedDb();
        const params = new HttpParams()
          .set('pattern', pattern)
          .set('database', database)
          .set('count', 100);
        this.http
          .get<{ keys: { key: string }[]; nextCursor: string | null }>(
            API_ROUTES.STORAGE.SCAN(this.connectionId()),
            { params }
          )
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: preview => {
              const examples =
                preview.keys.slice(0, 5).map(item => item.key).join(', ') ||
                '(none in this scan page)';
              this.dialog
                .open(ConfirmDialogComponent, {
                  data: {
                    title: 'Confirm Delete',
                    message: `Delete all keys matching "${pattern}" in DB ${database}? Preview (one scan page): ${examples}. This preview may be partial and keys may change before or during deletion.`,
                    confirmText: 'Delete',
                    cancelText: 'Cancel',
                    confirmColor: 'warn'
                  }
                })
                .afterClosed()
                .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
                .subscribe(() => {
                  void this.runPatternBulk(pattern, database);
                });
            },
            error: () =>
              this.snackBar.open(
                'Could not preview matching keys; deletion was not started.',
                'Close',
                { duration: 5000 }
              )
          });
      });
  }

  async exportData(format: 'json' | 'csv'): Promise<void> {
    const items = this.getSelectedItems();
    if (items.length === 0) {
      this.snackBar.open('Select at least one key to export.', 'Close', { duration: 3000 });
      return;
    }
    const hasBinary = items.some(item => !!item.keyBase64);
    const { ExportDialogComponent } = await import('../export/export-dialog.component');
    const ref = this.dialog.open(ExportDialogComponent, {
      closeOnNavigation: false,
      data: {
        connectionId: this.connectionId(),
        format,
        database: this.selectedDb(),
        pattern: this.searchPattern(),
        keys: items.map(item => item.key),
        ...(hasBinary ? { keysBase64: items.map(item => item.keyBase64 ?? null) } : {})
      }
    });
    ref.afterClosed().subscribe(success => {
      if (success) this.snackBar.open('Export completed', 'Close', { duration: 3000 });
    });
  }

  async importData(): Promise<void> {
    const { ImportDialogComponent } = await import('../import/import-dialog.component');
    const dialogRef = this.dialog.open(ImportDialogComponent, {
      closeOnNavigation: false,
      data: { connectionId: this.connectionId(), database: this.selectedDb() }
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(success => {
        this.redisKeysStore.clearCache();
        if (success) {
          this.snackBar.open(
            `Imported ${success.importedCount} key(s); skipped ${success.skippedCount}.`,
            'Close',
            { duration: 3000 }
          );
        }
        this.loadKeys();
      });
  }

  renameKey(target: string | RedisKeyInfo): void {
    const item = this.resolveKeyItem(target);
    const oldKey = item?.key ?? (typeof target === 'string' ? target : target.key);
    const oldKeyBase64 = item?.keyBase64 ?? null;

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
          oldKeyBase64,
          newKey: newKey.trim(),
          database: this.selectedDb(),
          onSuccess: () => {
            this.snackBar.open(`Renamed "${oldKey}" to "${newKey}"`, 'Close', { duration: 3000 });
            this.loadKeys();
          }
        });
      });
  }

  duplicateKey(target: string | RedisKeyInfo): void {
    const item = this.resolveKeyItem(target);
    const sourceKey = item?.key ?? (typeof target === 'string' ? target : target.key);
    const sourceKeyBase64 = item?.keyBase64 ?? null;

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
          sourceKeyBase64,
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
    const items = this.getSelectedItems();
    const database = this.selectedDb();
    if (items.length === 0) return;

    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: `Set TTL for ${items.length} keys`,
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
        const input = val.trim();
        const seconds = Number(input);
        if (!/^\d+$/.test(input) || !Number.isSafeInteger(seconds)) {
          this.snackBar.open('Enter a whole number of seconds, or 0 to persist.', 'Close', {
            duration: 4000
          });
          return;
        }
        const ttlSeconds = seconds === 0 ? null : seconds;
        const preview = items.slice(0, 5).map(i => i.key).join(', ');

        this.dialog
          .open(ConfirmDialogComponent, {
            data: {
              title: 'Confirm TTL change',
              message: `${ttlSeconds === null ? 'Persist' : `Set TTL to ${ttlSeconds}s for`} ${items.length} key(s) in DB ${database}? Examples: ${preview}${items.length > 5 ? ', …' : ''}. Keys may change before this runs.`,
              confirmText: 'Apply',
              cancelText: 'Cancel'
            }
          })
          .afterClosed()
          .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
          .subscribe(() => {
            void this.runSelectedBulk('ttl', items, database, ttlSeconds);
          });
      });
  }

  private resolveKeyItem(target: string | RedisKeyInfo): RedisKeyInfo | undefined {
    if (typeof target !== 'string') return target;
    return (
      this.keysByIdentity().get(`txt:${target}`) ??
      this.keys().find(row => row.key === target)
    );
  }

  private getSelectedItems(): RedisKeyInfo[] {
    const map = this.keysByIdentity();
    const items: RedisKeyInfo[] = [];
    for (const id of this.selection.selected) {
      const found = map.get(id);
      if (found) {
        items.push(found);
      } else if (id.startsWith('txt:')) {
        items.push({ key: id.slice(4), type: 'string', ttl: null, size: 0, memoryUsage: null });
      }
    }
    return items;
  }

  private async runPatternBulk(pattern: string, database: number): Promise<void> {
    const { BulkJobProgressComponent } =
      await import('../bulk-job-progress/bulk-job-progress.component');
    this.dialog
      .open(BulkJobProgressComponent, {
        disableClose: true,
        closeOnNavigation: false,
        width: '480px',
        data: { connectionId: this.connectionId(), operation: 'delete-pattern', pattern, database }
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshAfterBulk());
  }

  private async runSelectedBulk(
    operation: 'delete' | 'ttl',
    items: RedisKeyInfo[],
    database: number,
    ttlSeconds?: number | null
  ): Promise<void> {
    const { BulkJobProgressComponent } =
      await import('../bulk-job-progress/bulk-job-progress.component');
    const hasBinary = items.some(item => !!item.keyBase64);
    this.dialog
      .open(BulkJobProgressComponent, {
        disableClose: true,
        closeOnNavigation: false,
        width: '480px',
        data: {
          connectionId: this.connectionId(),
          operation,
          keys: items.map(item => item.key),
          ...(hasBinary ? { keysBase64: items.map(item => item.keyBase64 ?? null) } : {}),
          database,
          ttlSeconds
        }
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshAfterBulk());
  }

  private refreshAfterBulk(): void {
    this.selection.clear();
    this.redisKeysStore.clearCache();
    this.loadKeys();
  }

  async seedDemoData(): Promise<void> {
    const { SeedDataDialogComponent } =
      await import('../seed-data-dialog/seed-data-dialog.component');
    this.dialog
      .open(SeedDataDialogComponent, {
        width: '740px',
        maxWidth: '92vw',
        closeOnNavigation: false,
        data: {
          connectionId: this.connectionId(),
          database: this.selectedDb()
        }
      })
      .afterClosed()
      .subscribe(seeded => {
        if (seeded)
          this.snackBar.open('Sample-data job finished. Keys refreshed.', 'Close', {
            duration: 3000
          });
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
      closeOnNavigation: false,
      data: {
        connectionId: this.connectionId(),
        database: this.selectedDb()
      }
    });
  }
}
