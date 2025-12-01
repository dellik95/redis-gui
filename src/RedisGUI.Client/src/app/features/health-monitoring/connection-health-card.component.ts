import { Component, inject, input, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RedisOperationsService } from '../../services/api/redis-operations.service';
import { GetConnectionHealthResponse } from '../../models/redis-advanced.types';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-connection-health-card',
  standalone: true,
  templateUrl: './connection-health-card.component.html',
  styleUrls: ['./connection-health-card.component.scss'],
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule,
    MatTooltipModule,
    FormsModule
  ]
})
export class ConnectionHealthCardComponent {
  private api = inject(RedisOperationsService);

  connectionId = input.required<string>();

  health = signal<GetConnectionHealthResponse | null>(null);
  loading = signal<boolean>(false);
  lastError = signal<string | null>(null);
  autoRefreshMs = signal<number>(5000);
  latencyHistory = signal<number[]>([]); // store recent response times
  maxHistoryPoints = 50;
  active = signal<boolean>(true);

  // Start polling when component instantiated
  constructor() {
    this.startPolling();
  }

  private startPolling(): void {
    const tick = () => {
      if (!this.active()) return;
      this.refresh();
      setTimeout(tick, this.autoRefreshMs());
    };
    setTimeout(tick, 200); // initial slight delay
  }

  refresh(): void {
    this.loading.set(true);
    this.api.getConnectionHealth(this.connectionId()).subscribe({
      next: h => {
        this.health.set(h);
        this.lastError.set(null);
        if (h.responseTimeMs >= 0) {
          this.latencyHistory.update(list => {
            const updated = [...list, h.responseTimeMs];
            if (updated.length > this.maxHistoryPoints) {
              updated.splice(0, updated.length - this.maxHistoryPoints);
            }
            return updated;
          });
        }
      },
      error: err => {
        this.lastError.set(err.message || 'Unknown error');
      },
      complete: () => this.loading.set(false)
    });
  }

  stop(): void { this.active.set(false); }
  start(): void { if (!this.active()) { this.active.set(true); this.refresh(); this.startPolling(); } }
  clearHistory(): void { this.latencyHistory.set([]); }

  statusClass(): string {
    const h = this.health();
    if (!h) return 'unknown';
    const status = (h.status || '').toLowerCase();
    if (status.includes('healthy')) return 'healthy';
    if (status.includes('degraded')) return 'degraded';
    if (status.includes('unhealthy')) return 'unhealthy';
    return 'unknown';
  }

  svgPath(): string {
    const points = this.latencyHistory();
    if (points.length === 0) return '';
    const max = Math.max(...points, 1);
    const w = 200;
    const h = 40;
    const step = w / Math.max(points.length - 1, 1);
    let d = '';
    points.forEach((val, i) => {
      const x = i * step;
      const y = h - (val / max) * (h - 4) - 2; // padding
      d += i === 0 ? `M ${x} ${y}` : ` L ${x} ${y}`;
    });
    return d;
  }
}
