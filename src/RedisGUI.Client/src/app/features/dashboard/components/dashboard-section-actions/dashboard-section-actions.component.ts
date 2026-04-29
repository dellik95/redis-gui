import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { EditConnectionDialogService } from '../../services/edit-connection-dialog.service';

/**
 * Dashboard Section Actions Component
 * Action buttons displayed in the section actions portal on the dashboard
 * 
 * Features:
 * - Create new connection button
 * - Other dashboard-specific actions
 * 
 * This component is dynamically loaded into the section actions portal
 * via SectionActionsStore
 */
@Component({
  selector: 'app-dashboard-section-actions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDividerModule,
    MatIconModule
  ],
  templateUrl: './dashboard-section-actions.component.html',
  styleUrl: './dashboard-section-actions.component.scss'
})
export class DashboardSectionActionsComponent {
  // 1. Injected dependencies
  readonly connectionDialogService = inject(EditConnectionDialogService);
}
