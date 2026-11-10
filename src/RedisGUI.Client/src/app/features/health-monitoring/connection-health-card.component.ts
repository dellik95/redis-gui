import { Component, inject, input, signal, effect, computed, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { RedisOperationsStore } from '../../core/stores/redis-operations.store';
import { GetConnectionHealthResponse } from '../../models/redis-advanced.types';
import { UiSelectComponent, UiSelectOption } from '../../shared/ui';

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
    MatSelectModule,
    MatIconModule,
    MatTooltipModule,
    FormsModule,
    UiSelectComponent
  ]
})
export class ConnectionHealthCardComponent {
  // 1. Injected dependencies
  private readonly api = inject(RedisOperationsStore);
  private readonly destroyRef = inject(DestroyRef);

  // 2. Inputs
  readonly connectionId = input.required<string>();

  // 3. State signals
  readonly health = signal<GetConnectionHealthResponse | null>(null);
  readonly loading = signal<boolean>(false);
  readonly lastError = signal<string | null>(null);
  readonly autoRefreshMs = signal<number>(5000);
  readonly latencyHistory = signal<number[]>([]);
  readonly active = signal<boolean>(true);

  // 4. Configuration
  private readonly maxHistoryPoints = 50;
  private refreshTimeoutId: ReturnType<typeof setTimeout> | null = null;
  readonly intervalOptions: UiSelectOption[] = [
    { label: '3s', value: 3000 },
    { label: '5s', value: 5000 },
    { label: '10s', value: 10000 },
    { label: '30s', value: 30000 }
  ];

  // 5. Computed signals
  readonly statusClass = computed(() => {
    const h = this.health();
    if (!h) return 'unknown';
    const status = (h.status || '').toLowerCase();
    if (status.includes('healthy')) return 'healthy';
    if (status.includes('degraded')) return 'degraded';
    if (status.includes('unhealthy')) return 'unhealthy';
    return 'unknown';
  });

  readonly gradientId = computed(() => 'latencyGlow_' + (this.connectionId() ? this.connectionId().replace(/[^a-zA-Z0-9]/g, '') : 'default'));

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

  // 6. Constructor with effects
  constructor() {
    // Watch store state for health updates and update local state
    effect(() => {
      const h = this.api.connectionHealth();
      if (h) {
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
        this.loading.set(false);
      }
      
      const err = this.api.error();
      if (err) {
        this.lastError.set(err);
        this.loading.set(false);
      }
    }, { allowSignalWrites: true });

    // Start polling when active
    effect(() => {
      if (this.active()) {
        this.startPolling();
      } else {
        this.stopPolling();
      }
    });

    // Cleanup on destroy
    this.destroyRef.onDestroy(() => {
      this.stopPolling();
    });
  }

  // 7. Public methods
  /**
   * Refresh health data
   */
  refresh(): void {
    this.loading.set(true);
    this.api.loadConnectionHealth({ connectionId: this.connectionId() });
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

  // 8. Private methods
  /**
   * Start polling for health updates
   */
  private startPolling(): void {
    this.stopPolling(); // Clear any existing polling
    
    const tick = () => {
      if (!this.active()) {
        this.stopPolling();
        return;
      }
      this.refresh();
      this.refreshTimeoutId = setTimeout(tick, this.autoRefreshMs());
    };
    
    // Initial delay before first refresh
    this.refreshTimeoutId = setTimeout(tick, 200);
  }

  /**
   * Stop polling for health updates
   */
  private stopPolling(): void {
    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId);
      this.refreshTimeoutId = null;
    }
  }
}
