import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { EditConnectionDialogService } from '../services/edit-connection-dialog.servce';

@Component({
  selector: 'app-dashboard-section-actions',
  standalone: true,
  imports: [
    MatButtonModule, MatDividerModule, MatIconModule
  ],
  templateUrl: './dashboard-section-actions.component.html',
  styleUrl: './dashboard-section-actions.component.scss'
})
export class DashboardSectionActionsComponent {
  readonly connectionDialogService: EditConnectionDialogService = inject(EditConnectionDialogService);
}
