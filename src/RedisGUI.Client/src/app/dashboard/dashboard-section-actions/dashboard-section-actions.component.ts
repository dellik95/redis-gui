import { Component, inject } from '@angular/core';
import { MatButton, MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIcon, MatIconModule } from '@angular/material/icon';
import { RedisConnectionsService } from '../../../shared/services/redis-connections.service';
import { catchError } from 'rxjs';

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
  connectionService = inject(RedisConnectionsService);

  addClick() {
    this.connectionService.saveConnection({
      name: "string",
      host: "string",
      port: 0,
      database: 0,
      username: "string",
      password: "string"
    })
      .pipe(catchError(error => {
        return error;
      }))
      .subscribe();
  }

}
