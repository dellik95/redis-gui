import { AfterViewInit, Component, inject, input, Input, ViewChild } from '@angular/core';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { RedisConnectionsDataSource } from './redis-connections-datasource';
import { ColumnDefinitionType, SectionConfigType } from '../../../shared/types/section-config.type';
import { ConnectionType } from '../../../shared/types/connection.type';
import { CommonModule } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatButtonModule, MatFabButton } from '@angular/material/button';
import { RedisConnectionsService } from '../../../shared/services/redis-connections.service';
import { takeLast } from 'rxjs';

@Component({
  selector: 'app-redis-connections',
  templateUrl: './redis-connections-list.component.html',
  styleUrls: ['./redis-connections-list.component.scss'],
  standalone: true,
  imports: [MatTableModule, MatPaginatorModule, MatSortModule, CommonModule, MatIcon, MatButtonModule]
})
export class RedisConnectionsListComponent implements AfterViewInit {

  config = input.required<SectionConfigType<ConnectionType>>();
  connectionService = inject(RedisConnectionsService);
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatTable) table!: MatTable<ConnectionType>;
  dataSource = new RedisConnectionsDataSource(this.connectionService);

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
