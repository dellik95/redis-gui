import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';

import { MatIcon } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';

import { EditConnectionDialogService } from '../../services/edit-connection-dialog.service';
import { ConnectionsStore } from '../../../../core/stores/connections.store';
import { SideNavStore } from '../../../../core/stores/side-nav.store';
import { Connection as ConnectionType } from '../../../../models/connection.type';
import { ConfirmDialogComponent, EmptyStateComponent } from '../../../../shared/components';

/**
 * Storage Connections List Component
 * Displays interactive cards and table of Redis connections with actions
 */
@Component({
  selector: 'app-connections',
  standalone: true,
  templateUrl: './storage-connections-list.component.html',
  styleUrl: './storage-connections-list.component.scss',
  imports: [MatIcon, MatButtonModule, MatTooltipModule, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageConnectionsListComponent {
  // 1. Injected dependencies
  readonly connectionsStore = inject(ConnectionsStore);
  private readonly sideNavStore = inject(SideNavStore);
  readonly connectionDialogService = inject(EditConnectionDialogService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  constructor() {
    if (this.connectionsStore.needsRefresh()) this.connectionsStore.loadConnections();
  }

  /**
   * Adds a new connection
   */
  addConnection(): void {
    this.connectionDialogService.showAddConnectionDialog();
  }

  /**
   * Deletes a connection after user confirms.
   */
  deleteRecord(id: string, event: Event): void {
    event.stopPropagation();
    const conn = this.connectionsStore.connections().find(c => c.id === id);
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete Connection',
          message: conn
            ? `Are you sure you want to delete "${conn.name}" (${conn.host}:${conn.port})? This cannot be undone.`
            : 'Are you sure you want to delete this connection? This cannot be undone.',
          confirmText: 'Delete',
          cancelText: 'Cancel',
          confirmColor: 'warn'
        },
        width: '420px'
      })
      .afterClosed()
      .subscribe((confirmed: boolean) => {
        if (confirmed) {
          this.connectionsStore.deleteConnection(id);
        }
      });
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
    this.router.navigate(['/storage', connection.id]);
  }
}
