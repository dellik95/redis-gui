import {
  Component,
  inject,
  input,
  output,
  signal,
  computed,
  DestroyRef,
  ChangeDetectionStrategy
} from '@angular/core';
import { httpResource } from '@angular/common/http';
import { API_ROUTES } from '../../../../core/constants/api-routes.constants';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  StorageValueEditorComponent,
  ValueEditorInput,
  ValueEditorOutput
} from '../storage-value-editor/storage-value-editor.component';
import { SelectedRedisKey, RedisValue, toRedisDataType } from '../../../../models/redis-keys.types';
import { RedisKeysStore } from '../../../../core/stores/redis-keys.store';
import { EmptyStateComponent } from '../../../../shared/components';

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
    MatTooltipModule,
    StorageValueEditorComponent,
    EmptyStateComponent
  ],
  templateUrl: './edit-key.component.html',
  styleUrl: './edit-key.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditKeyComponent {
  // 1. Injected dependencies
  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly redisKeysStore = inject(RedisKeysStore);

  // 2. Inputs
  /** Redis connection ID */
  readonly connectionId = input.required<string>();

  /** Currently selected key to edit */
  readonly selectedKey = input<SelectedRedisKey | null>(null);

  // 3. Outputs
  /** Emitted when editor is closed */
  readonly closed = output<void>();

  /** Emitted when key is successfully saved */
  readonly saved = output<void>();

  // 4. State signals
  /** Saving state during update operation */
  readonly isSaving = signal<boolean>(false);

  // 5. Angular 22 Resource API for declarative key loading
  readonly keyResource = httpResource<{
    key: string;
    value: RedisValue;
    type: string;
    ttl?: number;
    timeToLive?: string;
  }>(() => {
    const key = this.selectedKey();
    if (!key || !this.connectionId()) return undefined;
    return {
      url: API_ROUTES.STORAGE.GET_VALUE(this.connectionId(), key.key),
      params: { database: String(this.redisKeysStore.activeDatabase()) }
    };
  });

  readonly isLoading = this.keyResource.isLoading;
  readonly loadError = this.keyResource.error;
  readonly editorData = computed<ValueEditorInput | null>(() => {
    if (!this.keyResource.hasValue()) return null;
    const response = this.keyResource.value();
    let ttl = response.ttl ?? -1;
    if (ttl === -1 && response.timeToLive) {
      const parts = response.timeToLive.split(':');
      if (parts.length === 3)
        ttl = Math.round(Number(parts[0]) * 3600 + Number(parts[1]) * 60 + Number(parts[2]));
    }
    return {
      key: response.key,
      value: response.value,
      type: toRedisDataType(response.type || this.selectedKey()?.type || 'string'),
      ttl,
      isNew: false
    };
  });

  refreshKey(): void {
    this.keyResource.reload();
    this.snackBar.open('Key value refreshed from Redis', 'Close', { duration: 2000 });
  }

  copyKeyName(): void {
    const key = this.selectedKey()?.key;
    if (key) {
      navigator.clipboard.writeText(key);
      this.snackBar.open(`Copied "${key}" to clipboard`, 'Close', { duration: 2000 });
    }
  }

  onSave(data: ValueEditorOutput): void {
    if (this.isSaving()) return;
    this.isSaving.set(true);
    this.redisKeysStore.updateValue({
      connectionId: this.connectionId(),
      key: data.key,
      type: data.type,
      value: data.value,
      ...(data.ttl !== undefined ? { ttl: data.ttl } : {}),
      database: this.redisKeysStore.activeDatabase(),
      onError: () => this.isSaving.set(false),
      onSuccess: () => {
        this.redisKeysStore.clearCache();
        if (this.destroyRef.destroyed) return;
        this.snackBar.open('Key updated successfully', 'Close', { duration: 3000 });
        this.isSaving.set(false);
        this.keyResource.reload();
        this.saved.emit();
      }
    });
  }

  onClose(): void {
    this.closed.emit();
  }
}
