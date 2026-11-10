import { DataSource } from '@angular/cdk/collections';
import { from, Observable, of } from 'rxjs';
import { Connection as ConnectionType } from '../../../../models/connection.type';
import { inject } from '@angular/core';
import { ConnectionsStore } from '../../../../core/stores/connections.store';
import { toObservable } from '@angular/core/rxjs-interop';

export interface RedisConnectionsItem {
  name: string;
  id: number;
}

/**
 * DataSource for storage connections table
 * Uses signals directly instead of BehaviorSubject for better performance
 */
export class StorageConnectionsDataSource extends DataSource<ConnectionType> {
  private readonly connectionsStore = inject(ConnectionsStore);
  private readonly $data = toObservable(this.connectionsStore.connections);

  constructor() {
    super();
    // Load connections if needed
    if (this.connectionsStore.needsRefresh()) {
      this.connectionsStore.loadConnections();
    }
  }

  connect(): Observable<ConnectionType[]> {
    return this.$data;
  }

  disconnect(): void {
    // No manual cleanup needed - toObservable handles it automatically
  }

  /**
   * Reloads connections data
   * Triggers a refresh if needed
   */
  reloadData(): void {
    if (this.connectionsStore.needsRefresh()) {
      this.connectionsStore.loadConnections();
    }
  }
}
