import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTable, MatTableModule } from '@angular/material/table';
import { MatTreeModule } from '@angular/material/tree';
import { TogglerComponent, TogglerConfig } from '../../../shared/components/toggler/toggler.component';
import { StorageValuesDataSource } from '../services/storage-values.datasource';
import { TreeViewListComponent } from '../../../shared/components/tree-view-list/tree-view-list.component';

@Component({
  selector: 'app-storage-values-list',
  templateUrl: './storage-values-list.component.html',
  styleUrls: ['./storage-values-list.component.scss'],
  imports: [
    MatTableModule, 
    MatTreeModule, 
    MatIconModule, 
    CommonModule, 
    MatButtonModule, 
    TogglerComponent, 
    TreeViewListComponent]
})
export class StorageValuesListComponent implements OnInit {

  displayedColumns: string[] = ['type', 'key', 'ttl', 'size'];
  
  viewMode = signal("table");
  keyDelimiter: string = ':';

  readonly dataSource = new StorageValuesDataSource();
  readonly table = viewChild.required(MatTable);


  modesTogglerConfig: TogglerConfig = {
    defaultValue: "table",
    items: [
      {
        value: "table",
        icon: "format_list_bulleted"
      },
      {
        value: "tree",
        icon: "account_tree"
      }
    ]
  }

  ngOnInit(): void {
    console.log("NgOnInit");
  }

  ngOnDestroy() {
    console.log("ngOnDestroy");
  }

  ngAfterViewInit(): void {
    this.dataSource.connect().subscribe(data => {
      this.table().dataSource = data;
    });
  }

  toggleViewMode(mode: string): void {
    this.viewMode.set(mode);
  }


  changeKeyDelimiter(): void {
    this.keyDelimiter = this.keyDelimiter === ':' ? '/' : ':';
  }

  addNewKey(): void {

  }
}
