import { AfterViewInit, Component, inject, input, viewChild, OnInit, DestroyRef } from '@angular/core';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { StorageConnectionsDataSource } from './storage-connections-datasource';
import { ColumnDefinition as ColumnDefinitionType, SectionConfig as SectionConfigType } from '../../../../models/section-config.type';
import { Connection as ConnectionType } from '../../../../models/connection.type';
import { CommonModule } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { StorageConnectionsService } from '../../../../services/api/storage-connections.service';
import { takeLast } from 'rxjs';
import { EditConnectionDialogService } from '../../services/edit-connection-dialog.servce';
import { ActivatedRoute, Router } from '@angular/router';
import { SideNavService } from '../../../../shared/services/side-nav.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-connections',
  templateUrl: './storage-connections-list.component.html',
  styleUrls: ['./storage-connections-list.component.scss'],
  imports: [MatTableModule, MatPaginatorModule, MatSortModule, CommonModule, MatIcon, MatButtonModule]
})
export class StorageConnectionsListComponent implements AfterViewInit, OnInit {
  readonly connectionService = inject(StorageConnectionsService);
  readonly dataSource = new StorageConnectionsDataSource(this.connectionService);
  readonly connectionDialogService = inject(EditConnectionDialogService);
  readonly router = inject(Router);
  readonly route = inject(ActivatedRoute);
  readonly sideNavService = inject(SideNavService);
  readonly destroyRef = inject(DestroyRef);

  readonly paginator = viewChild.required(MatPaginator);
  readonly sort = viewChild.required(MatSort);
  readonly table = viewChild.required(MatTable);

  config = input.required<SectionConfigType<ConnectionType>>();

  ngOnInit(): void {
    // Subscribe to connection updates from dialog
    this.connectionDialogService.connectionUpdated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.dataSource.reloadData();
      });
  }

  ngAfterViewInit(): void {
    this.dataSource.connect().subscribe(data => {
      this.table().dataSource = data;
    });
  }

  deleteRecord(id: string, event: Event) {
    event.stopPropagation();
    this.connectionService.deleteRecord(id).pipe(takeLast(1)).subscribe(() => {
      this.connectionService.clearCache();
      this.dataSource.reloadData();
    })
  }

  editRecord(connection: ConnectionType, event: Event) {
    event.stopPropagation();
    this.connectionDialogService.showEditConnectionDialog(connection);
  }

  rowClick(connection: ConnectionType) {
    // Update navigation with connection-specific routes
    this.sideNavService.addConnectionRoutes(connection.id, connection.name);
    this.router.navigate(["/storage", connection.id]);
  }

  getColumnValue(definition: ColumnDefinitionType<ConnectionType>, row: any): any {
    if (definition?.transformer) {
      return definition?.transformer(row);
    }
    return row[definition.name];
  }

  getColumns() {
    return [...this.config()];
  }

  getDisplayedColumns() {
    return [...this.config()?.map(c => c.name), "actions"];
  }
}
