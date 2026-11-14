import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';

import { ConnectionsStore } from '../../core/stores/connections.store';
import {
  ConnectionHealthCardComponent,
  ConnectionHealthStatus,
  ConnectionHealthUpdate
} from './connection-health-card.component';
import { UiSelectComponent, UiToggleComponent } from '../../shared/ui';
import { PageHeaderComponent, StatSummaryCardComponent, EmptyStateComponent } from '../../shared/components';

/**
 * Health Monitoring Dashboard Component
 *
 * Displays real-time health status for all Redis connections.
 *
 * Features:
 * - Overview of all connections health
 * - Individual connection health cards
 * - Auto-refresh capability
 * - Health history tracking
 * - Modern signal-based reactivity
 * - Zoneless compatible
 */
@Component({
  selector: 'app-health-monitoring',
  standalone: true,
  imports: [
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatChipsModule,
    ConnectionHealthCardComponent,
    UiSelectComponent,
    UiToggleComponent,
    PageHeaderComponent,
    StatSummaryCardComponent,
    EmptyStateComponent
  ],
  templateUrl: './health-monitoring.component.html',
  styleUrl: './health-monitoring.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HealthMonitoringComponent {
  // 1. Injected dependencies
  readonly connectionsStore = inject(ConnectionsStore);

  // 2. Store signals
  readonly connections = this.connectionsStore.connections;
  readonly isLoading = this.connectionsStore.isLoading;
  readonly error = this.connectionsStore.error;

  // 3. State signals
  readonly autoRefreshEnabled = signal(true);
  readonly refreshIntervalMs = signal(5000);
  readonly refreshToken = signal(0);
  readonly healthStatusByConnection = signal<Record<string, ConnectionHealthStatus>>({});

  // 4. Configuration
  readonly refreshIntervals = [
    { label: '3 seconds', value: 3000 },
    { label: '5 seconds', value: 5000 },
    { label: '10 seconds', value: 10000 },
    { label: '30 seconds', value: 30000 },
    { label: '1 minute', value: 60000 }
  ] as const;

  // 5. Computed signals
  readonly healthSummary = computed(() => {
    const statuses = this.healthStatusByConnection();
    return this.connections().reduce<Record<ConnectionHealthStatus, number>>(
      (summary, connection) => {
        summary[statuses[connection.id] ?? 'unknown'] += 1;
        return summary;
      },
      { healthy: 0, degraded: 0, unhealthy: 0, unknown: 0 }
    );
  });

  readonly overallStatusIcon = computed(() => {
    if (this.healthSummary().unhealthy > 0) return 'error';
    if (this.healthSummary().degraded > 0) return 'warning';
    if (this.healthSummary().healthy > 0) return 'check_circle';
    return 'help';
  });

  readonly overallStatusClass = computed(() => {
    if (this.healthSummary().unhealthy > 0) return 'unhealthy';
    if (this.healthSummary().degraded > 0) return 'degraded';
    if (this.healthSummary().healthy > 0) return 'healthy';
    return 'unknown';
  });

  constructor() {
    // Load connections if not already loaded
    if (this.connectionsStore.needsRefresh()) {
      this.connectionsStore.loadConnections();
    }
  }
  /**
   * Refresh all connections
   */
  refreshAll(): void {
    this.connectionsStore.loadConnections();
    this.refreshToken.update(token => token + 1);
  }

  onHealthUpdated(update: ConnectionHealthUpdate): void {
    this.healthStatusByConnection.update(statuses => ({
      ...statuses,
      [update.connectionId]: update.status
    }));
  }
}
