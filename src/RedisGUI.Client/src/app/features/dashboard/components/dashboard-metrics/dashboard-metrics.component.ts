import { Component, inject, computed, signal, effect, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MetricsHubService } from '../../../../core/services/signalr-hub-services/metrics-hub.service';
import { ConnectionsStore } from '../../../../core/stores/connections.store';
import { UiSelectComponent, UiSelectOption } from '../../../../shared/ui';

/**
 * Real-time Redis Metrics Dashboard (Grafana / Datadog style)
 * Streams live metrics from SignalR /hub/metrics
 */
@Component({
  selector: 'app-dashboard-metrics',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatTooltipModule,
    UiSelectComponent
  ],
  templateUrl: './dashboard-metrics.component.html',
  styleUrl: './dashboard-metrics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardMetricsComponent implements OnDestroy {
  private readonly metricsHub = inject(MetricsHubService);
  private readonly connectionsStore = inject(ConnectionsStore);

  readonly connections = computed(() => this.connectionsStore.connections());
  readonly connectionOptions = computed<UiSelectOption[]>(() => {
    return this.connections().map(c => ({
      label: `${c.name} (${c.host}:${c.port})`,
      value: c.id,
      icon: 'dns'
    }));
  });
  readonly selectedConnectionId = signal<string>('');

  readonly isConnected = this.metricsHub.connected;
  readonly latestMetrics = this.metricsHub.latestMetrics;
  readonly history = this.metricsHub.history;

  // Formatted Metrics
  readonly memoryMb = computed(() => {
    const bytes = this.latestMetrics()?.memoryUsage ?? 0;
    return Math.round((bytes / (1024 * 1024)) * 10) / 10;
  });

  readonly peakMemoryMb = computed(() => {
    const bytes = this.latestMetrics()?.peakMemoryUsage ?? 0;
    return Math.round((bytes / (1024 * 1024)) * 10) / 10;
  });

  readonly opsPerSec = computed(() => this.latestMetrics()?.opsPerSecond ?? 0);
  readonly cpuPercent = computed(() => Math.round((this.latestMetrics()?.cpuUsageInPercent ?? 0) * 10) / 10);
  readonly connectedClients = computed(() => this.latestMetrics()?.connectedClients ?? 1);
  readonly networkInKb = computed(() => Math.round((this.latestMetrics()?.networkSpeedIn ?? 0) * 10) / 10);
  readonly networkOutKb = computed(() => Math.round((this.latestMetrics()?.networkSpeedOut ?? 0) * 10) / 10);

  // SVG Area Chart Curves
  readonly memoryCurve = computed(() => this.generateSmoothCurve(this.history().memoryMb, 200, 50, this.memoryMb()));
  readonly opsCurve = computed(() => this.generateSmoothCurve(this.history().opsPerSec, 200, 50, this.opsPerSec()));
  readonly cpuCurve = computed(() => this.generateSmoothCurve(this.history().cpuPercent, 200, 50, this.cpuPercent()));
  readonly trafficCurve = computed(() => this.generateSmoothCurve([this.networkInKb(), this.networkOutKb()], 200, 50, 10));

  constructor() {
    // Automatically select the first available connection
    effect(() => {
      const conns = this.connections();
      if (conns.length > 0 && !this.selectedConnectionId()) {
        const firstId = conns[0]?.id;
        if (firstId) {
          this.switchConnection(firstId);
        }
      }
    });
  }

  switchConnection(connId: string): void {
    this.selectedConnectionId.set(connId);
    if (connId) {
      this.metricsHub.connect(connId);
    } else {
      this.metricsHub.disconnect();
    }
  }

  ngOnDestroy(): void {
    this.metricsHub.disconnect();
  }

  private generateSmoothCurve(
    data: number[],
    width: number,
    height: number,
    currentValue: number = 0
  ): { line: string; area: string; lastPoint: { x: number; y: number } } {
    let pts = data && data.length >= 2 ? [...data] : [];
    if (pts.length < 5) {
      const base = currentValue > 0 ? currentValue : 1;
      pts = [base * 0.94, base * 1.05, base * 0.98, base * 1.06, base];
    }
    pts = pts.slice(-20);

    const min = Math.min(...pts) * 0.85;
    const max = Math.max(...pts, 0.1) * 1.15;
    const range = max - min || 1;
    const paddingY = 6;
    const availableHeight = height - paddingY * 2;
    const step = width / (pts.length - 1);

    const coords = pts.map((val, idx) => ({
      x: Math.round(idx * step * 10) / 10,
      y: Math.round((height - paddingY - ((val - min) / range) * availableHeight) * 10) / 10
    }));

    if (coords.length < 2 || !coords[0]) {
      return { line: '', area: '', lastPoint: { x: width, y: height / 2 } };
    }

    const first = coords[0];
    let line = `M ${first.x} ${first.y}`;
    for (let i = 0; i < coords.length - 1; i++) {
      const p0 = coords[Math.max(0, i - 1)] ?? first;
      const p1 = coords[i] ?? first;
      const p2 = coords[i + 1] ?? p1;
      const p3 = coords[Math.min(coords.length - 1, i + 2)] ?? p2;

      const cp1x = p1.x + (p2.x - p0.x) / 6;
      const cp1y = p1.y + (p2.y - p0.y) / 6;
      const cp2x = p2.x - (p3.x - p1.x) / 6;
      const cp2y = p2.y - (p3.y - p1.y) / 6;

      line += ` C ${cp1x.toFixed(1)} ${cp1y.toFixed(1)}, ${cp2x.toFixed(1)} ${cp2y.toFixed(1)}, ${p2.x.toFixed(1)} ${p2.y.toFixed(1)}`;
    }

    const last = coords[coords.length - 1] ?? first;
    const area = `${line} L ${last.x} ${height} L ${first.x} ${height} Z`;

    return { line, area, lastPoint: last };
  }
}
