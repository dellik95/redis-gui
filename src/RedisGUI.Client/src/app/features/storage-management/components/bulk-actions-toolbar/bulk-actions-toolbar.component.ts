import { Component, input, output, ChangeDetectionStrategy, computed } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

/**
 * Bulk Actions Toolbar Component
 * Toolbar for bulk operations on Redis keys
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
  readonly selectedCount = input<number>(0);
  readonly pattern = input<string>('*');
  readonly loading = input<boolean>(false);

  readonly deleteSelected = output<void>();
  readonly deleteByPattern = output<void>();
  readonly exportJson = output<void>();
  readonly exportCsv = output<void>();
  readonly importData = output<void>();
  readonly clearSelection = output<void>();
  readonly setBulkTtl = output<void>();

  readonly hasSelection = computed(() => this.selectedCount() > 0);
}
