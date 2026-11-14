import {
  Component,
  inject,
  signal,
  input,
  effect,
  DestroyRef,
  ChangeDetectionStrategy
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { StorageResourceDetailsService } from '../../services/storage-resource-details.service';
import { PORTAL_DATA } from '../../../../core/stores/section-actions.store';
import { SystemMetrics } from '../../models/system-metrics.type';
import { FormatBytesPipe } from '../../../../shared/pipes/format-bytes.pipe';
import { DecimalPipe } from '@angular/common';
import { switchMap, catchError, EMPTY } from 'rxjs';

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
 * Can be used as a standalone component with [connectionId] or loaded via portal
 *
 * @example
 * ```html
 * <app-storage-resources-info [connectionId]="id()" />
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
  private readonly data = inject<any>(PORTAL_DATA, { optional: true });
  private readonly destroyRef = inject(DestroyRef);

  // 2. Direct inputs
  readonly connectionId = input<string>();

  // 3. State signals
  /** Current system metrics from SignalR */
  readonly resourceInfo = signal<SystemMetrics | null>(null);

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.resourceDetailsService.stopConnection().subscribe({ error: () => {} });
    });

    effect(() => {
      const directId = this.connectionId();
      const rawData = this.data;
      const portalId = rawData instanceof Map ? (rawData.get('id') ?? '') : (rawData?.id ?? '');
      const id = directId || portalId;
      if (!id) return;

      // Instantaneous metrics for zero-delay display — silently ignore 400
      // (connection exists in DB but hasn't opened an endpoint yet)
      this.resourceDetailsService
        .getInstantMetrics(id)
        .pipe(
          catchError(() => EMPTY),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe(metrics => {
          if (metrics) this.resourceInfo.set(metrics);
        });

      // Real-time live metrics via SignalR
      this.resourceDetailsService
        .startConnection(id)
        .pipe(
          switchMap(() => this.resourceDetailsService.receiveMessage()),
          catchError(err => {
            console.warn('Realtime metrics connection error:', err);
            return EMPTY;
          }),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe(metrics => {
          this.resourceInfo.set(metrics);
        });
    });
  }
}
