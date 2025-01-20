import { AfterViewInit, Component, inject, input, viewChild } from '@angular/core';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { StorageConnectionsDataSource } from './storage-connections-datasource';
import { ColumnDefinition as ColumnDefinitionType, SectionConfig as SectionConfigType } from '../../../shared/types/section-config.type';
import { Connection as ConnectionType } from '../../../shared/types/connection.type';
import { CommonModule } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { StorageConnectionsService } from '../../../shared/services/storage-connections.service';
import { takeLast } from 'rxjs';
import { EditConnectionDialogService } from '../services/edit-connection-dialog.servce';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-connections',
  templateUrl: './storage-connections-list.component.html',
  styleUrls: ['./storage-connections-list.component.scss'],
  imports: [MatTableModule, MatPaginatorModule, MatSortModule, CommonModule, MatIcon, MatButtonModule]
})
export class StorageConnectionsListComponent implements AfterViewInit {
  readonly connectionService = inject(StorageConnectionsService);
  readonly dataSource = new StorageConnectionsDataSource(this.connectionService);
  readonly connectionDialogService = inject(EditConnectionDialogService);
  readonly router = inject(Router);
  readonly route = inject(ActivatedRoute);

  readonly paginator = viewChild.required(MatPaginator);
  readonly sort = viewChild.required(MatSort);
  readonly table = viewChild.required(MatTable);

  config = input.required<SectionConfigType<ConnectionType>>();

  ngAfterViewInit(): void {
    this.dataSource.connect().subscribe(data => {
      this.table().dataSource = data;
    });
  }

  deleteRecord(id: string, event: Event) {
    event.stopPropagation();
    this.connectionService.deleteRecord(id).pipe(takeLast(1)).subscribe(() => {
      this.dataSource.reloadData();
    })
  }

  editRecord(connection: ConnectionType, event: Event) {
    event.stopPropagation();
    this.connectionDialogService.showEditConnectionDialog(connection);
  }

  rowClick(id: string) {
    this.router.navigate(["/storage", id]);
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
