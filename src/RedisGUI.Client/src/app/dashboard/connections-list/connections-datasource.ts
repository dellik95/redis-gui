import { DataSource } from '@angular/cdk/collections';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
import { Connection as ConnectionType } from '../../../shared/types/connection.type';
import { inject } from '@angular/core';
import { RedisConnectionsService } from '../../../shared/services/redis-connections.service';

export interface RedisConnectionsItem {
  name: string;
  id: number;
}

export class RedisConnectionsDataSource extends DataSource<ConnectionType> {
  private dataSubject = new BehaviorSubject<ConnectionType[]>([]);
  subscription: Subscription | null = null;

  constructor(private connectionService: RedisConnectionsService) {
    super();
    this.reloadData();
  }

  connect(): Observable<ConnectionType[]> {
    return this.dataSubject.asObservable();
  }

  disconnect(): void {
    this.dataSubject.complete();
  }

  reloadData() {
    if (this.subscription != null) {
      this.subscription.unsubscribe();
    }
    this.loadData();
  }

  private loadData(): void {
    this.subscription = this.connectionService.getConnections().subscribe(
      (data: ConnectionType[]) => {
        this.dataSubject.next(data);
      },
      () => {
        this.dataSubject.next([]);
      }
    );
  }
}
