import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';

import { ConfirmDialogComponent, PromptDialogComponent } from '../../../../shared/components';
import { RedisKeysStore } from '../../../../core/stores/redis-keys.store';
import { RedisOperationsStore } from '../../../../core/stores/redis-operations.store';
import { ValueEditorOutput } from '../storage-value-editor/storage-value-editor.component';

/**
 * Service that encapsulates all dialog-driven key operations for the storage list.
 * Keeps StorageValuesListComponent free of imperative dialog orchestration.
 */
@Injectable()
export class KeyActionsService {
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly redisKeysStore = inject(RedisKeysStore);
  private readonly redisOperationsStore = inject(RedisOperationsStore);
  private readonly destroyRef = inject(DestroyRef);

  /** Open add-key dialog and optionally seed with a folder prefix. */
  async openAddKey(connectionId: string, database: number, folderPrefix = '', onSuccess?: () => void): Promise<void> {
    const { AddKeyComponent } = await import('../add-key/add-key.component');
    this.dialog
      .open(AddKeyComponent, {
        width: '900px',
        maxWidth: '92vw',
        maxHeight: '90vh',
        panelClass: 'custom-key-dialog',
        data: { connectionId, folderPrefix }
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result: ValueEditorOutput | undefined) => {
        if (!result) return;
        this.redisKeysStore.addValue({
          connectionId,
          key: result.key,
          type: result.type,
          value: result.value,
          database,
          ...(result.ttl !== undefined ? { ttl: result.ttl } : {}),
          onSuccess: () => {
            this.redisKeysStore.clearCache();
            onSuccess?.();
            this.snackBar.open(`Key "${result.key}" created successfully`, 'Close', { duration: 3000 });
          }
        });
      });
  }

  /** Delete a single key with confirmation. */
  deleteKey(connectionId: string, key: string, database: number, onSuccess?: () => void): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete Key',
          message: `Are you sure you want to delete the key "${key}"?`,
          confirmText: 'Delete',
          cancelText: 'Cancel',
          confirmColor: 'warn'
        },
        width: '420px'
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        this.redisKeysStore.deleteKey({
          connectionId, key, database,
          onSuccess: () => {
            this.redisKeysStore.clearCache();
            onSuccess?.();
            this.snackBar.open(`Key "${key}" deleted`, 'Close', { duration: 3000 });
          }
        });
      });
  }

  /** Delete selected keys with confirmation. */
  deleteSelected(connectionId: string, keys: string[], database: number, onSuccess?: () => void): void {
    if (keys.length === 0) {
      this.snackBar.open('No keys selected', 'Close', { duration: 3000 });
      return;
    }
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete Selected Keys',
          message: `Are you sure you want to delete ${keys.length} selected key(s)?`,
          confirmText: 'Delete',
          cancelText: 'Cancel',
          confirmColor: 'warn'
        },
        width: '420px'
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        this.redisOperationsStore.deleteMultipleKeys({
          connectionId, keys, database,
          onSuccess: () => {
            this.redisKeysStore.clearCache();
            onSuccess?.();
            this.snackBar.open(`Deleted ${keys.length} key(s)`, 'Close', { duration: 4000 });
          }
        });
      });
  }

  /** Delete keys matching a pattern (prompts for pattern then confirms). */
  deleteByPattern(connectionId: string, currentPattern: string, database: number, onSuccess?: () => void): void {
    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: 'Delete by Pattern',
          message: 'Enter pattern to delete (e.g., user:*, session:*):',
          placeholder: 'Pattern',
          defaultValue: currentPattern,
          required: true
        }
      })
      .afterClosed()
      .pipe(filter((p): p is string => !!p?.trim()))
      .subscribe(pattern => {
        this.dialog
          .open(ConfirmDialogComponent, {
            data: {
              title: 'Confirm Delete by Pattern',
              message: `Delete all keys matching "${pattern}"? This cannot be undone.`,
              confirmText: 'Delete',
              cancelText: 'Cancel',
              confirmColor: 'warn'
            },
            width: '420px'
          })
          .afterClosed()
          .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
          .subscribe(() => {
            this.redisOperationsStore.deleteKeysByPattern({
              connectionId, pattern, database,
              onSuccess: () => {
                this.redisKeysStore.clearCache();
                onSuccess?.();
                this.snackBar.open(`Deleted keys matching "${pattern}"`, 'Close', { duration: 4000 });
              }
            });
          });
      });
  }

  /** Rename a key via prompt. */
  renameKey(connectionId: string, oldKey: string, database: number, onSuccess?: () => void): void {
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
      .pipe(filter((k): k is string => !!k?.trim() && k !== oldKey))
      .subscribe(newKey => {
        this.redisKeysStore.renameKey({
          connectionId, oldKey, newKey: newKey.trim(), database,
          onSuccess: () => {
            onSuccess?.();
            this.snackBar.open(`Renamed "${oldKey}" → "${newKey}"`, 'Close', { duration: 3000 });
          }
        });
      });
  }

  /** Duplicate a key via prompt. */
  duplicateKey(connectionId: string, sourceKey: string, database: number, onSuccess?: () => void): void {
    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: 'Duplicate Key',
          message: 'Enter name for the cloned key:',
          placeholder: 'Target key name',
          defaultValue: `${sourceKey}_copy`,
          required: true
        }
      })
      .afterClosed()
      .pipe(filter((k): k is string => !!k?.trim()))
      .subscribe(targetKey => {
        this.redisKeysStore.duplicateKey({
          connectionId, sourceKey, targetKey: targetKey.trim(), database,
          onSuccess: () => {
            onSuccess?.();
            this.snackBar.open(`Cloned "${sourceKey}" as "${targetKey}"`, 'Close', { duration: 3000 });
          }
        });
      });
  }

  /** Set TTL on multiple keys via prompt. */
  setBulkTtl(connectionId: string, keys: string[], database: number, onSuccess?: () => void): void {
    if (keys.length === 0) return;
    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: `Set TTL for ${keys.length} key(s)`,
          message: 'Enter TTL in seconds (0 = persist indefinitely):',
          placeholder: 'TTL in seconds',
          defaultValue: '300',
          required: true
        }
      })
      .afterClosed()
      .pipe(filter((v): v is string => !!v?.trim()))
      .subscribe(val => {
        const ttlSec = parseInt(val, 10);
        if (isNaN(ttlSec)) return;
        this.redisKeysStore.bulkUpdateTtl({
          connectionId,
          keys,
          ttlSeconds: ttlSec <= 0 ? null : ttlSec,
          database,
          onSuccess: (count: number) => {
            onSuccess?.();
            this.snackBar.open(
              ttlSec <= 0
                ? `${count} key(s) persisted indefinitely`
                : `TTL set to ${ttlSec}s on ${count} key(s)`,
              'Close', { duration: 3000 }
            );
          }
        });
      });
  }

  /** Delete a folder (namespace) with all its keys. */
  deleteFolder(
    connectionId: string,
    event: { path: string; name: string; keyCount: number; keys: string[] },
    delimiter: string,
    database: number,
    onSuccess?: () => void
  ): void {
    const pattern = event.path.endsWith(delimiter)
      ? `${event.path}*`
      : `${event.path}${delimiter}*`;

    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: `Delete Folder "${event.name}"`,
          message: `Delete folder "${event.path}" and all ${event.keyCount} key(s) under it?\nThis will delete all keys matching "${pattern}". This cannot be undone.`,
          confirmText: `Delete ${event.keyCount} Key(s)`,
          cancelText: 'Cancel',
          confirmColor: 'warn'
        },
        width: '460px'
      })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        const action = event.keys.length > 0
          ? this.redisOperationsStore.deleteMultipleKeys({
              connectionId, keys: event.keys, database,
              onSuccess: () => {
                this.redisKeysStore.clearCache();
                onSuccess?.();
                this.snackBar.open(`Folder "${event.name}" and ${event.keys.length} key(s) deleted`, 'Close', { duration: 3500 });
              }
            })
          : this.redisOperationsStore.deleteKeysByPattern({
              connectionId, pattern, database,
              onSuccess: () => {
                this.redisKeysStore.clearCache();
                onSuccess?.();
                this.snackBar.open(`Folder "${event.name}" keys deleted`, 'Close', { duration: 3500 });
              }
            });
      });
  }

  /** Open command palette. */
  async openCommandPalette(options: {
    connectionId: string;
    dialog: MatDialog;
    onAddNewKey: () => void;
    onSeedData: () => void;
    onOpenSlowlog: () => void;
    onOpenLoadTest: () => void;
    onExport: () => void;
    onImport: () => void;
    onRefresh: () => void;
    onSelectKey: (k: string) => void;
    onSwitchDb: (db: number) => void;
  }): Promise<void> {
    if (options.dialog.getDialogById('command-palette')) return;
    const { CommandPaletteComponent } =
      await import('../../../../shared/components/command-palette/command-palette.component');
    options.dialog.open(CommandPaletteComponent, {
      id: 'command-palette',
      width: '680px',
      maxWidth: '94vw',
      panelClass: 'command-palette-dialog-panel',
      enterAnimationDuration: '0ms',
      exitAnimationDuration: '0ms',
      data: {
        connectionId: options.connectionId,
        onAddNewKey: options.onAddNewKey,
        onSeedData: options.onSeedData,
        onOpenSlowlog: options.onOpenSlowlog,
        onOpenLoadTest: options.onOpenLoadTest,
        onExport: options.onExport,
        onImport: options.onImport,
        onRefresh: options.onRefresh,
        onSelectKey: options.onSelectKey,
        onSwitchDb: options.onSwitchDb
      }
    });
  }

  /** Open seed data dialog. */
  async openSeedData(connectionId: string): Promise<void> {
    const { SeedDataDialogComponent } = await import('../seed-data-dialog/seed-data-dialog.component');
    this.dialog.open(SeedDataDialogComponent, {
      width: '680px',
      maxWidth: '92vw',
      data: { connectionId }
    });
  }

  /** Open slowlog dialog. */
  async openSlowlog(connectionId: string): Promise<void> {
    const { SlowlogDialogComponent } = await import('../slowlog-dialog/slowlog-dialog.component');
    this.dialog.open(SlowlogDialogComponent, {
      width: '800px',
      maxWidth: '94vw',
      data: { connectionId }
    });
  }

  /** Open load test dialog. */
  async openLoadTest(connectionId: string): Promise<void> {
    const { LoadTestDialogComponent } = await import('../load-test-dialog/load-test-dialog.component');
    this.dialog.open(LoadTestDialogComponent, {
      width: '720px',
      maxWidth: '94vw',
      data: { connectionId }
    });
  }

  /** Open export dialog. */
  async openExport(connectionId: string, format: 'json' | 'csv', onSuccess?: () => void): Promise<void> {
    const { ExportDialogComponent } = await import('../export/export-dialog.component');
    this.dialog
      .open(ExportDialogComponent, { data: { connectionId, format } })
      .afterClosed()
      .subscribe(success => {
        if (success) this.snackBar.open('Export completed', 'Close', { duration: 3000 });
      });
  }

  /** Open import dialog. */
  async openImport(connectionId: string, onSuccess?: () => void): Promise<void> {
    const { ImportDialogComponent } = await import('../import/import-dialog.component');
    this.dialog
      .open(ImportDialogComponent, { data: { connectionId } })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(success => {
        if (success) {
          this.redisKeysStore.clearCache();
          onSuccess?.();
          this.snackBar.open('Data imported successfully', 'Close', { duration: 3000 });
        }
      });
  }
}
