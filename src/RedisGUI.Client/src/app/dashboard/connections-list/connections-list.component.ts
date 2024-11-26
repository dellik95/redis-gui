import { AfterViewInit, Component, inject, input, ViewChild } from '@angular/core';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { RedisConnectionsDataSource } from './connections-datasource';
import { ColumnDefinition as ColumnDefinitionType, SectionConfig as SectionConfigType } from '../../../shared/types/section-config.type';
import { Connection as ConnectionType } from '../../../shared/types/connection.type';
import { CommonModule } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { RedisConnectionsService } from '../../../shared/services/redis-connections.service';
import { takeLast } from 'rxjs';
import { EditConnectionDialogService } from '../services/edit-connection-dialog.servce';

@Component({
  selector: 'app-connections',
  templateUrl: './connections-list.component.html',
  styleUrls: ['./connections-list.component.scss'],
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatSortModule, CommonModule, MatIcon, MatButtonModule]
})
export class ConnectionsListComponent implements AfterViewInit {
  readonly connectionService = inject(RedisConnectionsService);
  readonly dataSource = new RedisConnectionsDataSource(this.connectionService);
  readonly connectionDialogService = inject(EditConnectionDialogService)

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatTable) table!: MatTable<ConnectionType>;

  config = input.required<SectionConfigType<ConnectionType>>();

  ngAfterViewInit(): void {
    this.dataSource.connect().subscribe(data => {
      this.table.dataSource = data;
    });
  }

  deleteRecord(id: string) {
    this.connectionService.deleteRecord(id).pipe(takeLast(1)).subscribe(() => {
      this.dataSource.reloadData();
    })
    console.log("Id:", id)
  }

  editRecord(connection: ConnectionType) {
    this.connectionDialogService.showEditConnectionDialog(connection);
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
