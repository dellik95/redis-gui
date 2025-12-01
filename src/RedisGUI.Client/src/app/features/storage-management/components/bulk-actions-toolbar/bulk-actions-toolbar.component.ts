import { Component, EventEmitter, Output, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-bulk-actions-toolbar',
  standalone: true,
  templateUrl: './bulk-actions-toolbar.component.html',
  styleUrls: ['./bulk-actions-toolbar.component.scss'],
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule
  ]
})
export class BulkActionsToolbarComponent {
  @Input() selectedCount: number = 0;
  @Input() pattern: string = '*';
  @Input() loading: boolean = false;

  @Output() deleteSelected = new EventEmitter<void>();
  @Output() deleteByPattern = new EventEmitter<void>();
  @Output() exportJson = new EventEmitter<void>();
  @Output() exportCsv = new EventEmitter<void>();
  @Output() importData = new EventEmitter<void>();
  @Output() clearSelection = new EventEmitter<void>();

  hasSelection(): boolean { return this.selectedCount > 0; }
}
