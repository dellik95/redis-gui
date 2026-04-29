import { Component, inject, input, output, signal, effect, ChangeDetectionStrategy, DestroyRef } from '@angular/core';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { StorageValueEditorComponent, ValueEditorInput, ValueEditorOutput } from '../storage-value-editor/storage-value-editor.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RedisDataType, RedisValue } from '../../../../models/redis-keys.types';
import { RedisKeysStore } from '../../../../core/stores/redis-keys.store';

/**
 * Edit Key Component
 * Sidebar panel for editing existing Redis keys
 *
 * Features:
 * - Loads key value from Redis
 * - Displays value in editor
 * - Updates key value and TTL
 * - Emits save/close events
 *
 * @example
 * ```html
 * <app-edit-key
 *   [connectionId]="connectionId"
 *   [selectedKey]="selectedKey"
 *   (saved)="onKeySaved()"
 *   (closed)="onEditorClosed()"
 * />
 * ```
 */
@Component({
  selector: 'app-edit-key',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    StorageValueEditorComponent
  ],
  templateUrl: './edit-key.component.html',
  styleUrl: './edit-key.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditKeyComponent {
  // 1. Injected dependencies
  private readonly snackBar = inject(MatSnackBar);
  private readonly redisKeysStore = inject(RedisKeysStore);
  private readonly destroyRef = inject(DestroyRef);

  // 2. Inputs
  /** Redis connection ID */
  readonly connectionId = input.required<string>();

  /** Currently selected key to edit */
  readonly selectedKey = input<{ key: string; type: RedisDataType; value: RedisValue; ttl: number } | null>(null);

  // 3. Outputs
  /** Emitted when editor is closed */
  readonly closed = output<void>();

  /** Emitted when key is successfully saved */
  readonly saved = output<void>();

  // 4. State signals
  /** Loading state while fetching key value */
  readonly isLoading = signal<boolean>(false);

  /** Saving state during update operation */
  readonly isSaving = signal<boolean>(false);

  /** Editor input data */
  readonly editorData = signal<ValueEditorInput | null>(null);

  constructor() {
    // Watch for selected key changes and load the value
    effect(() => {
      const key = this.selectedKey();
      if (key) {
        this.loadKeyValue(key);
      } else {
        this.editorData.set(null);
      }
    });

    // Watch for loaded value from store
    effect(() => {
      const loadedKey = this.redisKeysStore.selectedKey();
      const currentKey = this.selectedKey();
      const storeLoading = this.redisKeysStore.isLoading();
      const storeError = this.redisKeysStore.error();

      if (loadedKey && currentKey && loadedKey.key === currentKey.key) {
        // loadedKey.type is already RedisDataType from the store
        this.editorData.set({
          key: currentKey.key,
          type: this.mapTypeToRedisDataType(loadedKey.type),
          value: loadedKey.value,
          ttl: loadedKey.ttl ?? -1,
          isNew: false
        });
        this.isLoading.set(false);
      } else if (currentKey && !storeLoading && storeError) {
        // Loading failed - show error and stop loading
        this.isLoading.set(false);
        this.snackBar.open(`Failed to load key: ${storeError}`, "Close", { duration: 5000 });
      } else if (currentKey && !storeLoading && !loadedKey) {
        // Loading completed but no key found (shouldn't happen, but handle gracefully)
        this.isLoading.set(false);
      }
    });
  }

  // 5. Private methods
  /**
   * Maps Redis data type to RedisDataType enum
   * Handles both string and enum types from the store
   *
   * @param type - Redis data type (string or RedisDataType)
   * @returns RedisDataType enum value
   */
  private mapTypeToRedisDataType(type: string | RedisDataType): RedisDataType {
    // If already a RedisDataType enum, return as-is
    if (Object.values(RedisDataType).includes(type as RedisDataType)) {
      return type as RedisDataType;
    }

    // Otherwise, convert from string
    switch (String(type).toLowerCase()) {
      case 'string': return RedisDataType.String;
      case 'list': return RedisDataType.List;
      case 'set': return RedisDataType.Set;
      case 'hash': return RedisDataType.Hash;
      case 'zset':
      case 'sortedset': return RedisDataType.SortedSet;
      case 'stream': return RedisDataType.Stream;
      default: return RedisDataType.String;
    }
  }

  /**
   * Loads key value from Redis
   *
   * @param key - Key information to load
   */
  private loadKeyValue(key: { key: string; type: RedisDataType; value: RedisValue; ttl: number }): void {
    this.isLoading.set(true);
    // Call store directly - effect will handle updates when store changes
    this.redisKeysStore.loadValue({
      connectionId: this.connectionId(),
      key: key.key
    });
  }

  /**
   * Handles save action from value editor
   *
   * @param data - Editor output with updated key data
   */
  onSave(data: ValueEditorOutput): void {
    this.isSaving.set(true);
    // Call store directly
    this.redisKeysStore.updateValue({
      connectionId: this.connectionId(),
      key: data.key,
      type: data.type,
      value: data.value,
      ...(data.ttl !== undefined ? { ttl: data.ttl } : {})
    });
    // Clear cache to force refresh on next load, but don't reload immediately
    // The data source will automatically update when store changes
    this.redisKeysStore.clearCache();
    this.snackBar.open("Key updated successfully", "Close", { duration: 3000 });
    this.isSaving.set(false);
    this.saved.emit();
  }

  /**
   * Handles close action
   */
  onClose(): void {
    this.closed.emit();
  }
}

