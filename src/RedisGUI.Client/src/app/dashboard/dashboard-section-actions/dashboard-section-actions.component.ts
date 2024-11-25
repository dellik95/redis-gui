import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { RedisConnectionsService } from '../../../shared/services/redis-connections.service';
import { MatDialog } from '@angular/material/dialog';
import { EditConnectionComponent } from '../edit-connection/edit-connection.component';
import { takeLast } from 'rxjs';

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
  readonly connectionService = inject(RedisConnectionsService);
  readonly dialog = inject(MatDialog);


  editConnection() {
    this.openDialog('300ms', '150ms');
  }


  private openDialog(enterAnimationDuration: string, exitAnimationDuration: string): void {
    let dialogRef = this.dialog.open(EditConnectionComponent, {
      enterAnimationDuration,
      exitAnimationDuration,
    });

    dialogRef.afterClosed().pipe(takeLast(1)).subscribe(result => {
      // TODO: Add reload for table
    });
  }


}
