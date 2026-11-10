import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpClient } from '@angular/common/http';

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
    CommonModule,
    MatDialogModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  template: `
    <div class="dialog-header">
      <div class="title-row">
        <mat-icon class="slowlog-icon">history_toggle_off</mat-icon>
        <span class="title-text">Redis Slowlog Inspector</span>
      </div>
      <div class="header-actions">
        <button mat-icon-button (click)="loadSlowlog()" matTooltip="Refresh Slowlog" [disabled]="loading()">
          <mat-icon [class.spinning]="loading()">refresh</mat-icon>
        </button>
        <button mat-icon-button mat-dialog-close matTooltip="Close">
          <mat-icon>close</mat-icon>
        </button>
      </div>
    </div>

    <mat-dialog-content class="dialog-content">
      @if (loading()) {
        <div class="loading-state">
          <mat-spinner diameter="36"></mat-spinner>
          <span>Fetching slow query log from Redis...</span>
        </div>
      } @else if (entries().length === 0) {
        <div class="empty-state">
          <div class="empty-icon-circle">
            <mat-icon class="empty-icon">check_circle</mat-icon>
          </div>
          <h3>No Slow Queries Detected</h3>
          <p>Your Redis instance is operating within optimal response time thresholds.</p>
        </div>
      } @else {
        <div class="table-scroll">
          <table mat-table [dataSource]="entries()" class="slowlog-table">
            <!-- ID Column -->
            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef class="id-col"> #ID </th>
              <td mat-cell *matCellDef="let row" class="id-col"> #{{ row.id }} </td>
            </ng-container>

            <!-- Timestamp Column -->
            <ng-container matColumnDef="timestamp">
              <th mat-header-cell *matHeaderCellDef class="time-col"> Time </th>
              <td mat-cell *matCellDef="let row" class="time-col"> {{ row.timestamp | date:'HH:mm:ss.SSS' }} </td>
            </ng-container>

            <!-- Duration Column -->
            <ng-container matColumnDef="duration">
              <th mat-header-cell *matHeaderCellDef class="duration-col"> Execution Time </th>
              <td mat-cell *matCellDef="let row" class="duration-col">
                <span class="duration-badge">{{ row.duration }}</span>
              </td>
            </ng-container>

            <!-- Command Column -->
            <ng-container matColumnDef="arguments">
              <th mat-header-cell *matHeaderCellDef class="cmd-col"> Command & Arguments </th>
              <td mat-cell *matCellDef="let row" class="cmd-cell">
                <code>{{ formatArgs(row.arguments) }}</code>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns; sticky: true"></tr>
            <tr mat-row *matRowDef="let row; columns: columns;"></tr>
          </table>
        </div>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end" class="dialog-actions">
      <span class="info-note">Showing top {{ entries().length }} slowest operations</span>
      <button type="button" class="btn-secondary" mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 18px 24px 14px 24px;
      margin: 0;
      border-bottom: 1px solid var(--color-border, #1e293b);
      color: var(--color-text, #f8fafc);

      .title-row {
        display: flex;
        align-items: center;
        gap: 10px;
        font-size: 18px;
        font-weight: 600;

        .slowlog-icon {
          color: #f59e0b;
          font-size: 24px;
          width: 24px;
          height: 24px;
        }
      }

      .header-actions {
        display: flex;
        align-items: center;
        gap: 4px;

        .spinning {
          animation: spin 1s linear infinite;
        }
      }
    }

    .dialog-content {
      padding: 24px !important;
      min-height: 260px;
      max-height: 520px;
      overflow-y: auto;
      box-sizing: border-box;
      background: var(--color-card, #0f172a);
      color: var(--color-text, #f8fafc);
    }

    .loading-state, .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      gap: 12px;
      color: var(--color-text-muted, #94a3b8);
      text-align: center;
    }

    .empty-icon-circle {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      background: rgba(16, 185, 129, 0.15);
      border: 1px solid rgba(16, 185, 129, 0.3);
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 8px;

      .empty-icon {
        font-size: 32px;
        width: 32px;
        height: 32px;
        color: #10b981;
      }
    }

    .empty-state h3 {
      margin: 0;
      font-size: 18px;
      font-weight: 600;
      color: var(--color-text, #f8fafc);
    }

    .empty-state p {
      margin: 0;
      font-size: 14px;
      color: var(--color-text-muted, #94a3b8);
      max-width: 440px;
      line-height: 1.5;
    }

    .table-scroll {
      max-height: 440px;
      overflow: auto;
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      background: var(--color-card, #0f172a);
    }

    .slowlog-table {
      width: 100%;
      background: transparent;

      th {
        font-size: 12px;
        font-weight: 600;
        text-transform: uppercase;
        background: var(--color-card-hover, #1e293b);
        color: var(--color-text-muted, #94a3b8);
        padding: 10px 14px;
        border-bottom: 1px solid var(--color-border, #1e293b);
      }

      td {
        padding: 8px 14px;
        font-size: 13px;
        color: var(--color-text, #f8fafc);
        border-bottom: 1px solid var(--color-border, #1e293b);
      }
    }

    .id-col {
      width: 60px;
      font-weight: 600;
      color: var(--color-accent, #6366f1);
    }

    .time-col {
      width: 110px;
      font-family: monospace;
      color: var(--color-text-muted, #94a3b8);
    }

    .duration-col {
      width: 120px;
    }

    .duration-badge {
      background: rgba(245, 158, 11, 0.15);
      color: #f59e0b;
      border: 1px solid rgba(245, 158, 11, 0.3);
      font-weight: 600;
      padding: 3px 8px;
      border-radius: 6px;
      font-family: monospace;
      font-size: 12px;
    }

    .cmd-cell code {
      font-family: 'JetBrains Mono', 'Roboto Mono', monospace;
      font-size: 12px;
      background: rgba(255, 255, 255, 0.06);
      color: var(--color-text, #f8fafc);
      padding: 3px 6px;
      border-radius: 4px;
      word-break: break-all;
    }

    .dialog-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 14px 24px 18px 24px !important;
      margin: 0;
      border-top: 1px solid var(--color-border, #1e293b);
      background: var(--color-card, #0f172a);

      .info-note {
        font-size: 12px;
        color: var(--color-text-muted, #94a3b8);
      }
    }

    @keyframes spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SlowlogDialogComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly data = inject<{ connectionId: string }>(MAT_DIALOG_DATA);

  readonly entries = signal<SlowlogEntry[]>([]);
  readonly loading = signal<boolean>(true);
  readonly columns: readonly string[] = ['id', 'timestamp', 'duration', 'arguments'] as const;

  ngOnInit(): void {
    this.loadSlowlog();
  }

  loadSlowlog(): void {
    this.loading.set(true);
    const connId = this.data.connectionId;
    this.http.get<{ entries: SlowlogEntry[]; totalCount: number }>(`/api/v1/storage/${connId}/slowlog?count=25`)
      .subscribe({
        next: (res) => {
          this.entries.set(res?.entries || []);
          this.loading.set(false);
        },
        error: () => {
          this.entries.set([]);
          this.loading.set(false);
        }
      });
  }

  formatArgs(args: string[]): string {
    if (!args || args.length === 0) return '(none)';
    return args.join(' ');
  }
}
