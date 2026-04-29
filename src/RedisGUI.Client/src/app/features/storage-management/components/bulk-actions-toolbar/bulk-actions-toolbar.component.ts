import { Component, input, output, ChangeDetectionStrategy, computed } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

/**
 * Bulk Actions Toolbar Component
 * Toolbar for bulk operations on Redis keys
 * 
 * Features:
 * - Delete selected keys
 * - Delete by pattern
 * - Export to JSON/CSV
 * - Import data
 * - Clear selection
 * 
 * @example
 * ```html
 * <app-bulk-actions-toolbar
 *   [selectedCount]="selectedKeys.length"
 *   [pattern]="searchPattern"
 *   [loading]="isLoading"
 *   (deleteSelected)="onDeleteSelected()"
 *   (exportJson)="onExportJson()"
 * />
 * ```
 */
@Component({
  selector: 'app-bulk-actions-toolbar',
  standalone: true,
  templateUrl: './bulk-actions-toolbar.component.html',
  styleUrl: './bulk-actions-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule
  ]
})
export class BulkActionsToolbarComponent {
  // 1. Inputs
  /** Number of selected keys */
  readonly selectedCount = input<number>(0);
  
  /** Current search pattern */
  readonly pattern = input<string>('*');
  
  /** Loading state */
  readonly loading = input<boolean>(false);

  // 2. Outputs
  /** Emitted when delete selected action is triggered */
  readonly deleteSelected = output<void>();
  
  /** Emitted when delete by pattern action is triggered */
  readonly deleteByPattern = output<void>();
  
  /** Emitted when export to JSON action is triggered */
  readonly exportJson = output<void>();
  
  /** Emitted when export to CSV action is triggered */
  readonly exportCsv = output<void>();
  
  /** Emitted when import data action is triggered */
  readonly importData = output<void>();
  
  /** Emitted when clear selection action is triggered */
  readonly clearSelection = output<void>();

  // 3. Computed signals
  /** Whether any keys are selected */
  readonly hasSelection = computed(() => this.selectedCount() > 0);
}
