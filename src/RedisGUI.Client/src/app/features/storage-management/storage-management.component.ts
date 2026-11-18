import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  computed,
  effect,
  input
} from '@angular/core';
import { StorageResourcesInfoComponent } from './components/storage-resources-info/storage-resources-info.component';
import { PageHeaderComponent } from '../../shared/components';
import { StorageValuesListComponent } from './components/storage-values-list/storage-values-list.component';
import { EditKeyComponent } from './components/edit-key/edit-key.component';
import { RedisCliDrawerComponent } from '../../shared/components/redis-cli-drawer/redis-cli-drawer.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { SectionActionsStore } from '../../core/stores/section-actions.store';
import { StorageManagementStore } from './stores/storage-management.store';
import { SelectedRedisKey } from '../../models/redis-keys.types';
import { RedisKeysStore } from '../../core/stores/redis-keys.store';
import { ConnectionsStore } from '../../core/stores/connections.store';

/**
 * Storage management component
 * Main container for Redis key-value operations
 */
@Component({
  selector: 'app-storage-management',
  standalone: true,
  imports: [
    PageHeaderComponent,
    StorageResourcesInfoComponent,
    StorageValuesListComponent,
    EditKeyComponent,
    RedisCliDrawerComponent,
    MatIconModule
  ],
  templateUrl: './storage-management.component.html',
  styleUrl: './storage-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageManagementComponent {
  private readonly sectionActionsStore = inject(SectionActionsStore);
  private readonly snackBar = inject(MatSnackBar);
  private readonly storageStore = inject(StorageManagementStore);
  private readonly redisKeysStore = inject(RedisKeysStore);
  private readonly connectionsStore = inject(ConnectionsStore);

  readonly id = input.required<string>();

  readonly selectedKey = signal<SelectedRedisKey | null>(null);
  readonly activeDb = computed(() => this.redisKeysStore.activeDatabase());

  readonly currentConnection = computed(() =>
    this.connectionsStore.connections().find(c => c.id === this.id())
  );
  readonly isReadOnly = computed(() => !!this.currentConnection()?.isReadOnly);
  readonly isCluster = computed(
    () => this.redisKeysStore.serverMode() === 'Cluster' || this.currentConnection()?.serverMode === 'Cluster'
  );

  readonly error = computed(() => this.storageStore.error());
  readonly hasValidConnection = computed(() => !!this.id());

  private lastActiveDb: number | null = null;

  constructor() {
    if (this.connectionsStore.needsRefresh()) this.connectionsStore.loadConnections();
    effect(() => {
      const connectionId = this.id();
      if (connectionId) {
        this.selectedKey.set(null);
        this.storageStore.initialize(connectionId);
      }
    });

    effect(() => {
      const db = this.activeDb();
      if (this.lastActiveDb !== null && this.lastActiveDb !== db) {
        this.selectedKey.set(null);
      }
      this.lastActiveDb = db;
    });

    effect(() => {
      const mode = this.redisKeysStore.serverMode();
      const connId = this.id();
      if (connId && (mode === 'Cluster' || mode === 'Standalone' || mode === 'Sentinel')) {
        this.connectionsStore.updateServerMode(connId, mode);
      }
    });

    effect(() => {
      const errorMsg = this.error();
      if (errorMsg) {
        this.snackBar.open(errorMsg, 'Close', {
          duration: 5000,
          horizontalPosition: 'center',
          verticalPosition: 'bottom',
          panelClass: ['error-snackbar']
        });
      }
    });
  }

  onKeySelected(event: SelectedRedisKey): void {
    this.selectedKey.set(event);
  }

  onEditClosed(): void {
    this.selectedKey.set(null);
  }

  onEditSaved(): void {
    this.storageStore.loadKeys();
  }

  ngOnDestroy(): void {
    this.sectionActionsStore.clear();
  }
}
