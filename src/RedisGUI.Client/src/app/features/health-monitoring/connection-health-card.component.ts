import {
  Component,
  input,
  output,
  signal,
  effect,
  computed,
  ChangeDetectionStrategy
} from '@angular/core';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { httpResource } from '@angular/common/http';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { GetConnectionHealthResponse } from '../../models/redis-advanced.types';
import { API_ROUTES } from '../../core/constants/api-routes.constants';
import { MetricItemComponent } from '../../shared/components';

export type ConnectionHealthStatus = 'healthy' | 'degraded' | 'unhealthy' | 'unknown';

export interface ConnectionHealthUpdate {
  connectionId: string;
  status: ConnectionHealthStatus;
}

@Component({
  selector: 'app-connection-health-card',
  standalone: true,
  templateUrl: './connection-health-card.component.html',
  styleUrl: './connection-health-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    NgClass,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MetricItemComponent
  ]
})
export class ConnectionHealthCardComponent {
  // 1. Inputs
  readonly connectionId = input.required<string>();
  readonly pollingActive = input(true);
  readonly refreshIntervalMs = input(5000);
  readonly refreshToken = input(0);
  readonly healthUpdated = output<ConnectionHealthUpdate>();

  // 2. State signals
  readonly latencyHistory = signal<number[]>([]);
  readonly active = signal<boolean>(true);

  /** Per-card request state. It cannot be overwritten by another connection card. */
  readonly healthResource = httpResource<GetConnectionHealthResponse>(() => {
    const connectionId = this.connectionId();
    return connectionId ? API_ROUTES.CONNECTIONS.HEALTH(connectionId) : undefined;
  });

  readonly health = computed(() =>
    this.healthResource.hasValue() ? this.healthResource.value() : null
  );
  readonly loading = this.healthResource.isLoading;
  readonly lastError = computed(() => {
    const error = this.healthResource.error();
    return error instanceof Error
      ? error.message
      : error
        ? 'Unable to load connection health.'
        : null;
  });

  // 3. Configuration
  private readonly maxHistoryPoints = 50;

  // 4. Computed signals
  readonly statusClass = computed(() => {
    const h = this.health();
    if (!h) return 'unknown';
    const status = (h.status || '').toLowerCase();
    if (status.includes('unhealthy')) return 'unhealthy';
    if (status.includes('degraded')) return 'degraded';
    if (status.includes('healthy')) return 'healthy';
    return 'unknown';
  });

  readonly gradientId = computed(
    () =>
      'latencyGlow_' +
      (this.connectionId() ? this.connectionId().replace(/[^a-zA-Z0-9]/g, '') : 'default')
  );

  readonly svgPath = computed(() => {
    let points = this.latencyHistory();
    const current = this.health()?.responseTimeMs ?? 0.3;
    if (points.length < 3) {
      points = [current * 0.9, current * 1.08, current * 0.94, current * 1.05, current];
    }
    const min = Math.min(...points) * 0.8;
    const max = Math.max(...points, 0.5) * 1.2;
    const range = max - min || 1;
    const w = 240;
    const h = 42;
    const step = w / (points.length - 1);

    const coords = points.map((val, i) => ({
      x: i * step,
      y: h - ((val - min) / range) * (h - 12) - 6
    }));

    if (coords.length < 2 || !coords[0]) {
      return '';
    }

    const first = coords[0];
    let d = `M ${first.x.toFixed(1)} ${first.y.toFixed(1)}`;
    for (let i = 0; i < coords.length - 1; i++) {
      const p0 = coords[Math.max(0, i - 1)] ?? first;
      const p1 = coords[i] ?? first;
      const p2 = coords[i + 1] ?? p1;
      const p3 = coords[Math.min(coords.length - 1, i + 2)] ?? p2;

      const cp1x = p1.x + (p2.x - p0.x) / 6;
      const cp1y = p1.y + (p2.y - p0.y) / 6;
      const cp2x = p2.x - (p3.x - p1.x) / 6;
      const cp2y = p2.y - (p3.y - p1.y) / 6;

      d += ` C ${cp1x.toFixed(1)} ${cp1y.toFixed(1)}, ${cp2x.toFixed(1)} ${cp2y.toFixed(1)}, ${p2.x.toFixed(1)} ${p2.y.toFixed(1)}`;
    }
    return d;
  });

  readonly svgAreaPath = computed(() => {
    const linePath = this.svgPath();
    if (!linePath) return '';
    return `${linePath} L 240 42 L 0 42 Z`;
  });

  // 5. Resource effects
  constructor() {
    effect(() => {
      const h = this.health();
      if (h) {
        if (h.responseTimeMs >= 0) {
          this.latencyHistory.update(list => {
            const updated = [...list, h.responseTimeMs];
            if (updated.length > this.maxHistoryPoints) {
              updated.splice(0, updated.length - this.maxHistoryPoints);
            }
            return updated;
          });
        }
        this.healthUpdated.emit({
          connectionId: this.connectionId(),
          status: this.statusClass()
        });
      }
    });

    let previousRefresh = 0;
    effect(() => {
      const token = this.refreshToken();
      if (token !== previousRefresh) this.refresh();
      previousRefresh = token;
    });

    effect(onCleanup => {
      if (!this.active() || !this.pollingActive() || !this.connectionId()) return;

      const intervalId = window.setInterval(
        () => this.healthResource.reload(),
        this.refreshIntervalMs()
      );

      onCleanup(() => window.clearInterval(intervalId));
    });
  }

  // 6. Public methods
  /**
   * Refresh health data
   */
  refresh(): void {
    this.healthResource.reload();
  }

  /**
   * Stop auto-refresh polling
   */
  stop(): void {
    this.active.set(false);
  }

  /**
   * Start auto-refresh polling
   */
  start(): void {
    if (!this.active()) {
      this.active.set(true);
      this.refresh();
    }
  }

  /**
   * Clear latency history
   */
  clearHistory(): void {
    this.latencyHistory.set([]);
  }
}
