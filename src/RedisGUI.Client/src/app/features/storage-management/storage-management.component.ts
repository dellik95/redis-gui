import { ChangeDetectionStrategy, Component, inject, Injector, signal, computed, effect, input } from '@angular/core';
import { StorageResourcesInfoComponent } from './components/storage-resources-info/storage-resources-info.component';
import { ComponentPortal } from '@angular/cdk/portal';
import { PORTAL_DATA } from '../../core/stores/section-actions.store';
import { StorageValuesListComponent } from "./components/storage-values-list/storage-values-list.component";
import { EditKeyComponent } from "./components/edit-key/edit-key.component";
import { MatSnackBar } from '@angular/material/snack-bar';
import { SectionActionsStore } from '../../core/stores/section-actions.store';
import { StorageManagementStore } from './stores/storage-management.store';
import { RedisDataType, RedisValue } from '../../models/redis-keys.types';
import { RedisKeysStore } from '../../core/stores/redis-keys.store';

/**
 * Storage management component
 * Main container for Redis key-value operations
 *
 * Features:
 * - Displays list of Redis keys
 * - Inline value editor for quick edits
 * - Storage statistics in section actions
 * - Type-safe value updates
 * - Unified state management via StorageManagementStore
 * - Modern Angular 21 with automatic route param binding
 */
@Component({
  selector: 'app-storage-management',
  imports: [StorageValuesListComponent, EditKeyComponent],
  templateUrl: './storage-management.component.html',
  styleUrl: './storage-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageManagementComponent {
  // 1. Injected dependencies
  private readonly sectionActionsStore = inject(SectionActionsStore);
  private readonly injector = inject(Injector);
  private readonly snackBar = inject(MatSnackBar);
  private readonly storageStore = inject(StorageManagementStore);
  private readonly redisKeysStore = inject(RedisKeysStore);

  // 2. Route parameters (automatic binding via withComponentInputBinding)
  /**
   * Redis connection ID from route parameter
   * Automatically bound from route via withComponentInputBinding()
   */
  readonly id = input.required<string>();

  // 3. State signals
  /**
   * Currently selected key for editing
   */
  readonly selectedKey = signal<{ key: string; type: RedisDataType; value: RedisValue; ttl: number } | null>(null);

  // 4. Computed signals
  /**
   * Current error state (from store)
   */
  readonly error = computed(() => this.storageStore.error());

  /**
   * Whether connection ID is valid
   */
  readonly hasValidConnection = computed(() => !!this.id());

  constructor() {
    // Initialize store when connection ID changes (signal-based effect)
    effect(() => {
      const connectionId = this.id();
      if (connectionId) {
        this.storageStore.initialize(connectionId);
        this.showStats(connectionId);
      }
    }, { allowSignalWrites: true });

    // Watch for errors and show snackbar
    effect(() => {
      const error = this.error();
      if (error) {
        this.snackBar.open(`Error: ${error}`, "Close", { duration: 5000 });
      }
    });
  }

  /**
   * Shows storage statistics in the section actions portal
   * Updates when connection ID changes
   */
  private showStats(connectionId: string): void {
    const dataMap = new Map<string, string>([
      ["id", connectionId]
    ]);

    const childInjector = this.createInjector<Map<string, string>>(dataMap);
    const portal = new ComponentPortal(StorageResourcesInfoComponent, null, childInjector);
    this.sectionActionsStore.attach(portal);
  }

  /**
   * Clears the section actions portal
   */
  clearStat(): void {
    this.sectionActionsStore.clear();
  }

  /**
   * Creates a child injector for portal data
   * @param data - Data to inject into portal component
   */
  private createInjector<T>(data: T): Injector {
    return Injector.create({
      providers: [
        {
          provide: PORTAL_DATA, useValue: data
        }
      ]
    });
  }

  /**
   * Handles key selection from the values list
   * @param keyData - The selected key data
   */
  onKeySelected(keyData: { key: string, type: RedisDataType, value: RedisValue, ttl: number }): void {
    this.selectedKey.set(keyData);
  }

  /**
   * Handles edit panel close
   */
  onEditClosed(): void {
    this.selectedKey.set(null);
  }

  /**
   * Handles edit panel save
   */
  onEditSaved(): void {
    // Clear selection to close edit panel
    // Keys will be refreshed automatically by the data source watching the store
    this.selectedKey.set(null);
  }
}
