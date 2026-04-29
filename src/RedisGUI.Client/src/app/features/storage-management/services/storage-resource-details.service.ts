import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SignalRHubService } from '../../../core/services/signalr-hub.service';
import { SystemMetrics } from '../models/system-metrics.type';
import { SIGNALR_HUBS, SIGNALR_METHODS } from '../../../core/constants/api-routes.constants';

/**
 * SignalR service for real-time storage resource metrics
 * Connects to the metrics hub and streams system metrics data
 * 
 * Matches backend: RedisMetricsHub and IClientMethods
 *
 * @example
 * ```typescript
 * private resourceService = inject(StorageResourceDetailsService);
 *
 * // Start connection
 * this.resourceService.startConnection('conn-123').subscribe(() => {
 *   this.resourceService.receiveMessage().subscribe(metrics => {
 *     console.log('Received metrics:', metrics);
 *   });
 * });
 *
 * // Stop connection
 * this.resourceService.stopConnection().subscribe();
 * ```
 */
@Injectable({
	providedIn: 'root'
})
export class StorageResourceDetailsService extends SignalRHubService<SystemMetrics> {
	protected override hubUrl = SIGNALR_HUBS.METRICS;
	protected override eventName = SIGNALR_METHODS.METRICS.NOTIFY_USER;

	/**
	 * Start connection with connection ID
	 * @param id - The Redis connection ID
	 * @returns Observable that completes when connected
	 */
	startConnection(id: string): Observable<void> {
		return this.connect({ connectionId: id });
	}

	/**
	 * Stop the connection
	 * @returns Observable that completes when disconnected
	 */
	stopConnection(): Observable<void> {
		return this.disconnect();
	}

	/**
	 * Receive system metrics messages
	 * @returns Observable stream of metrics
	 */
	receiveMessage(): Observable<SystemMetrics> {
		return this.onMessage();
	}

	protected override onConnected(): void {
		// Connection established - no action needed
	}

	protected override onDisconnected(): void {
		// Connection closed - no action needed
	}

	protected override onConnectionError(_error: unknown): void {
		// Error is handled by base class
	}
}

