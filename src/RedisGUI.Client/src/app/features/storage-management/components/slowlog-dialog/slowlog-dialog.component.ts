import { Component, inject, computed, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { API_ROUTES } from '../../../../core/constants/api-routes.constants';
import { httpResource } from '@angular/common/http';
import { EmptyStateComponent } from '../../../../shared/components';

export interface SlowlogEntry {
  id: number;
  timestamp: string;
  duration: string;
  arguments: string[];
  clientAddress: string;
  clientName: string;
}

@Component({
  selector: 'app-slowlog-dialog',
  standalone: true,
  imports: [
    DatePipe,
    MatDialogModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    EmptyStateComponent
  ],
  templateUrl: './slowlog-dialog.component.html',
  styleUrl: './slowlog-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SlowlogDialogComponent {
  private readonly data = inject<{ connectionId: string }>(MAT_DIALOG_DATA);

  private readonly slowlog = httpResource<{ entries: SlowlogEntry[]; totalCount: number }>(() =>
    API_ROUTES.STORAGE.SLOWLOG(this.data.connectionId, 25)
  );
  readonly entries = computed(() =>
    this.slowlog.hasValue() ? (this.slowlog.value().entries ?? []) : []
  );
  readonly loading = this.slowlog.isLoading;
  readonly loadError = this.slowlog.error;
  readonly columns = ['id', 'timestamp', 'duration', 'arguments'] as const;

  loadSlowlog(): void {
    this.slowlog.reload();
  }

  formatArgs(args: string[]): string {
    if (!args || args.length === 0) return '(none)';
    return args.join(' ');
  }
}
