import { Component, inject, input, signal, effect, computed, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { RedisOperationsStore } from '../../core/stores/redis-operations.store';
import { GetConnectionHealthResponse } from '../../models/redis-advanced.types';

@Component({
  selector: 'app-connection-health-card',
  standalone: true,
  templateUrl: './connection-health-card.component.html',
  styleUrl: './connection-health-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    NgClass,
    MatCardModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule,
    MatTooltipModule,
    FormsModule
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

  readonly svgPath = computed(() => {
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
