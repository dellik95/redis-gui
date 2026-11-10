import { Component, inject, input, output, signal, computed, resource, ChangeDetectionStrategy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { StorageValueEditorComponent, ValueEditorInput, ValueEditorOutput } from '../storage-value-editor/storage-value-editor.component';
import { RedisDataType, RedisValue } from '../../../../models/redis-keys.types';
import { RedisKeysStore } from '../../../../core/stores/redis-keys.store';

/**
 * Edit Key Component (Angular 22 Resource API)
 * Modern sidebar panel for editing existing Redis keys using native Angular 22 Resource API
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
  private readonly http = inject(HttpClient);
  private readonly redisKeysStore = inject(RedisKeysStore);

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
  /** Saving state during update operation */
  readonly isSaving = signal<boolean>(false);

  // 5. Angular 22 Resource API for declarative key loading
  readonly keyResource = resource<ValueEditorInput | null, { key: string; connectionId: string; requestedType: RedisDataType; database: number } | null>({
    params: () => {
      const key = this.selectedKey();
      const connId = this.connectionId();
      const db = this.redisKeysStore.activeDatabase();
      return key && connId ? { key: key.key, connectionId: connId, requestedType: key.type, database: db } : null;
    },
    loader: async ({ params }) => {
      if (!params) return null;

      try {
        const httpParams = { database: params.database.toString() };
        const response = await firstValueFrom(
          this.http.get<{ key: string; value: any; type: string; ttl?: number; timeToLive?: string }>(
            `/api/v1/storage/${params.connectionId}/${encodeURIComponent(params.key)}`,
            { params: httpParams }
          )
        );

        let ttlVal = response.ttl ?? -1;
        if (response.timeToLive && ttlVal === -1) {
          const parts = response.timeToLive.split(':');
          if (parts.length === 3) {
            ttlVal = Math.round(
              parseFloat(parts[0] ?? '0') * 3600 +
              parseFloat(parts[1] ?? '0') * 60 +
              parseFloat(parts[2] ?? '0')
            );
          }
        }

        return {
          key: response.key,
          type: this.mapTypeToRedisDataType(response.type || params.requestedType),
          value: response.value,
          ttl: ttlVal,
          isNew: false
        };
      } catch (err) {
        throw new Error(err instanceof Error ? err.message : 'Failed to fetch key value');
      }
    }
  });

  // 6. Computed signals from Resource
  readonly isLoading = computed(() => this.keyResource.isLoading());
  readonly editorData = computed(() => this.keyResource.value() ?? null);
  readonly loadError = computed(() => this.keyResource.error());

  // 7. Methods
  private mapTypeToRedisDataType(type: string | RedisDataType): RedisDataType {
    if (Object.values(RedisDataType).includes(type as RedisDataType)) {
      return type as RedisDataType;
    }

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

  refreshKey(): void {
    this.keyResource.reload();
    this.snackBar.open("Key value refreshed from Redis", "Close", { duration: 2000 });
  }

  copyKeyName(): void {
    const key = this.selectedKey()?.key;
    if (key) {
      navigator.clipboard.writeText(key);
      this.snackBar.open(`Copied "${key}" to clipboard`, "Close", { duration: 2000 });
    }
  }

  onSave(data: ValueEditorOutput): void {
    this.isSaving.set(true);
    this.redisKeysStore.updateValue({
      connectionId: this.connectionId(),
      key: data.key,
      type: data.type,
      value: data.value,
      ...(data.ttl !== undefined ? { ttl: data.ttl } : {}),
      database: this.redisKeysStore.activeDatabase()
    });
    this.redisKeysStore.clearCache();
    this.snackBar.open("Key updated successfully", "Close", { duration: 3000 });
    this.isSaving.set(false);
    this.keyResource.reload();
    this.saved.emit();
  }

  onClose(): void {
    this.closed.emit();
  }
}
