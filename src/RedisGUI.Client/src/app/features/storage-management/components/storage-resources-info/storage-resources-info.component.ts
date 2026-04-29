import { Component, inject, signal, effect, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { StorageResourceDetailsService } from '../../services/storage-resource-details.service';
import { PORTAL_DATA } from '../../../../core/stores/section-actions.store';
import { SystemMetrics } from '../../models/system-metrics.type';
import { FormatBytesPipe } from '../../../../shared/pipes/format-bytes.pipe';
import { DecimalPipe } from '@angular/common';
import { switchMap } from 'rxjs';

/**
 * Storage Resources Info Component
 * Displays real-time storage resource metrics using SignalR
 * 
 * Features:
 * - Real-time memory usage
 * - Key count statistics
 * - Connection metrics
 * - Auto-updates via SignalR
 * 
 * This component is dynamically loaded into the section actions portal
 * 
 * @example
 * ```html
 * <app-storage-resources-info />
 * ```
 */
@Component({
	selector: 'app-storage-resources-info',
	standalone: true,
	imports: [MatIconModule, FormatBytesPipe, DecimalPipe],
	templateUrl: './storage-resources-info.component.html',
	styleUrl: './storage-resources-info.component.scss',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageResourcesInfoComponent {
	// 1. Injected dependencies
	private readonly resourceDetailsService = inject(StorageResourceDetailsService);
	private readonly data = inject<Map<string, string>>(PORTAL_DATA);
	private readonly destroyRef = inject(DestroyRef);

	// 2. State signals
	/** Current system metrics from SignalR */
	readonly resourceInfo = signal<SystemMetrics | null>(null);

	constructor() {
		effect(() => {
			const id = this.data.get('id') ?? '';
			if (!id) return;

			this.resourceDetailsService
				.startConnection(id)
				.pipe(
					switchMap(() => this.resourceDetailsService.receiveMessage()),
					takeUntilDestroyed(this.destroyRef)
				)
				.subscribe((metrics: SystemMetrics) => {
					this.resourceInfo.set(metrics);
				});
		});
	}
}
