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

  readonly error = computed(() => this.storageStore.error());
  readonly hasValidConnection = computed(() => !!this.id());

  constructor() {
    if (this.connectionsStore.needsRefresh()) this.connectionsStore.loadConnections();
    effect(() => {
      const connectionId = this.id();
      if (connectionId) {
        this.storageStore.initialize(connectionId);
      }
    });

    effect(() => {
      const errorMsg = this.error();
      if (errorMsg) {
        this.snackBar.open(errorMsg, 'Close', {
          duration: 5000,
          horizontalPosition: 'end',
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
