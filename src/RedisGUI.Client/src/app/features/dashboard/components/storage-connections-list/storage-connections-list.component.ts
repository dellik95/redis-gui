import { Component, inject, input, viewChild, effect, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatIcon } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

import { StorageConnectionsDataSource } from './storage-connections-datasource';
import { EditConnectionDialogService } from '../../services/edit-connection-dialog.service';
import { ConnectionsStore } from '../../../../core/stores/connections.store';
import { SideNavStore } from '../../../../core/stores/side-nav.store';
import { ColumnDefinition as ColumnDefinitionType, SectionConfig as SectionConfigType } from '../../../../models/section-config.type';
import { Connection as ConnectionType } from '../../../../models/connection.type';

/**
 * Storage Connections List Component
 * Displays a table of Redis connections with sorting, pagination, and actions
 * 
 * Features:
 * - Material table with sorting and pagination
 * - Connection management (edit, delete)
 * - Navigation to connection storage view
 * - Reactive updates from ConnectionsStore
 * 
 * @example
 * ```html
 * <app-connections [config]="connectionListConfig" />
 * ```
 */
@Component({
  selector: 'app-connections',
  standalone: true,
  templateUrl: './storage-connections-list.component.html',
  styleUrl: './storage-connections-list.component.scss',
  imports: [MatTableModule, MatPaginatorModule, MatSortModule, MatIcon, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageConnectionsListComponent {
  // 1. Injected dependencies
  private readonly connectionsStore = inject(ConnectionsStore);
  private readonly sideNavStore = inject(SideNavStore);
  private readonly connectionDialogService = inject(EditConnectionDialogService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  // 2. Inputs
  readonly config = input.required<SectionConfigType<ConnectionType>>();

  // 3. View queries
  readonly paginator = viewChild.required(MatPaginator);
  readonly sort = viewChild.required(MatSort);
  readonly table = viewChild.required(MatTable);

  // 4. Data source
  readonly dataSource = new StorageConnectionsDataSource();

  // 5. Constructor with effects
  constructor() {
    // React to connection updates
    effect(() => {
      const lastUpdate = this.connectionsStore.lastUpdateTimestamp();
      if (lastUpdate > 0) {
        this.dataSource.reloadData();
      }
    });

    // Connect data source when table is available
    effect(() => {
      const table = this.table();
      if (table) {
        this.dataSource.connect()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(data => {
            table.dataSource = data;
          });
      }
    });
  }

  // 6. TrackBy function for performance
  readonly trackById = (index: number, connection: ConnectionType) => connection.id;

  /**
   * Deletes a connection
   * 
   * @param id - Connection ID to delete
   * @param event - Click event (stopped to prevent row click)
   */
  deleteRecord(id: string, event: Event): void {
    event.stopPropagation();
    this.connectionsStore.deleteConnection(id);
    this.dataSource.reloadData();
  }

  /**
   * Opens edit dialog for a connection
   * 
   * @param connection - Connection to edit
   * @param event - Click event (stopped to prevent row click)
   */
  editRecord(connection: ConnectionType, event: Event): void {
    event.stopPropagation();
    this.connectionDialogService.showEditConnectionDialog(connection);
  }

  /**
   * Handles row click - navigates to connection storage view
   * 
   * @param connection - Connection that was clicked
   */
  rowClick(connection: ConnectionType): void {
    // Update navigation with connection-specific routes using store
    this.sideNavStore.addConnectionRoutes(connection.id, connection.name);
    this.router.navigate(["/storage", connection.id]);
  }

  /**
   * Get column value for a row, applying transformer if available
   */
  getColumnValue(definition: ColumnDefinitionType<ConnectionType>, row: ConnectionType): unknown {
    if (definition?.transformer) {
      return definition.transformer(row);
    }
    return row[definition.name as keyof ConnectionType];
  }

  /**
   * Get all column definitions
   */
  getColumns(): readonly ColumnDefinitionType<ConnectionType>[] {
    return [...this.config()];
  }

  /**
   * Get displayed column names including actions column
   */
  getDisplayedColumns(): readonly string[] {
    return [...this.config().map(c => c.name), "actions"] as const;
  }
}
