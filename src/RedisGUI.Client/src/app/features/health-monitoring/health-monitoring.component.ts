import { Component, inject, signal, computed, ChangeDetectionStrategy, effect } from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

import { ConnectionsStore } from '../../core/stores/connections.store';
import { RedisOperationsStore } from '../../core/stores/redis-operations.store';
import { ConnectionHealthCardComponent } from './connection-health-card.component';

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
		NgClass,
		FormsModule,
		RouterModule,
		MatCardModule,
		MatProgressSpinnerModule,
		MatIconModule,
		MatButtonModule,
		MatSelectModule,
		MatTooltipModule,
		MatChipsModule,
		MatSlideToggleModule,
		ConnectionHealthCardComponent
	],
	templateUrl: './health-monitoring.component.html',
	styleUrl: './health-monitoring.component.scss',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class HealthMonitoringComponent {
	// 1. Injected dependencies
	readonly connectionsStore = inject(ConnectionsStore);
	private readonly redisOperationsStore = inject(RedisOperationsStore);

	// 2. Store signals
	readonly connections = this.connectionsStore.connections;
	readonly isLoading = this.connectionsStore.isLoading;
	readonly error = this.connectionsStore.error;

	// 3. State signals
	readonly autoRefreshEnabled = signal(true);
	readonly refreshIntervalMs = signal(5000);
	readonly healthySummary = signal(0);
	readonly degradedSummary = signal(0);
	readonly unhealthySummary = signal(0);
	readonly unknownSummary = signal(0);

	// 4. Configuration
	readonly refreshIntervals = [
		{ label: '3 seconds', value: 3000 },
		{ label: '5 seconds', value: 5000 },
		{ label: '10 seconds', value: 10000 },
		{ label: '30 seconds', value: 30000 },
		{ label: '1 minute', value: 60000 }
	] as const;

	// 5. Computed signals
	readonly overallStatusIcon = computed(() => {
		if (this.unhealthySummary() > 0) return 'error';
		if (this.degradedSummary() > 0) return 'warning';
		if (this.healthySummary() > 0) return 'check_circle';
		return 'help';
	});

	readonly overallStatusClass = computed(() => {
		if (this.unhealthySummary() > 0) return 'unhealthy';
		if (this.degradedSummary() > 0) return 'degraded';
		if (this.healthySummary() > 0) return 'healthy';
		return 'unknown';
	});

	constructor() {
		// Load connections if not already loaded
		if (this.connectionsStore.needsRefresh()) {
			this.connectionsStore.loadConnections();
		}

		// Initialize summary stats based on connections count
		effect(() => {
			const conns = this.connections();
			// Initially all are unknown until health check completes
			this.unknownSummary.set(conns.length);
		}, { allowSignalWrites: true });
	}

	/**
	 * Refresh all connections
	 */
	refreshAll(): void {
		this.connectionsStore.loadConnections();
	}

	/**
	 * Toggle auto-refresh
	 */
	toggleAutoRefresh(): void {
		this.autoRefreshEnabled.update(v => !v);
	}
}
