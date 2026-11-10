import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { SIGNALR_HUBS, SIGNALR_METHODS } from '../../constants/api-routes.constants';

export interface RedisLiveMetrics {
  networkSpeedIn: number;
  networkSpeedOut: number;
  keyCount: number;
  memoryUsage: number;
  peakMemoryUsage: number;
  opsPerSecond: number;
  connectedClients: number;
  cpuUsageSystem: number;
  cpuUsageUser: number;
  cpuUsageInPercent: number;
  uptimeInSeconds: number;
}

/**
 * Service for streaming real-time Redis metrics from /hub/metrics
 */
@Injectable({ providedIn: 'root' })
export class MetricsHubService {
  private hubConnection: signalR.HubConnection | undefined = undefined;
  private readonly metricsSubject = new Subject<RedisLiveMetrics>();
  
  readonly connected = signal(false);
  readonly latestMetrics = signal<RedisLiveMetrics | null>(null);

  /** History array for sparkline charts (retains last 20 points) */
  readonly history = signal<{
    timestamps: string[];
    memoryMb: number[];
    opsPerSec: number[];
    cpuPercent: number[];
    clients: number[];
  }>({
    timestamps: [],
    memoryMb: [],
    opsPerSec: [],
    cpuPercent: [],
    clients: []
  });

  get metrics$(): Observable<RedisLiveMetrics> {
    return this.metricsSubject.asObservable();
  }

  connect(connectionId: string): void {
    this.disconnect();

    const url = `${SIGNALR_HUBS.METRICS}?connectionId=${connectionId}`;
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hubConnection.on(SIGNALR_METHODS.METRICS.NOTIFY_USER, (metrics: RedisLiveMetrics) => {
      this.latestMetrics.set(metrics);
      this.metricsSubject.next(metrics);
      this.appendHistory(metrics);
    });

    this.hubConnection
      .start()
      .then(() => {
        this.connected.set(true);
      })
      .catch(() => {
        this.connected.set(false);
      });

    this.hubConnection.onreconnecting(() => this.connected.set(false));
    this.hubConnection.onreconnected(() => this.connected.set(true));
    this.hubConnection.onclose(() => this.connected.set(false));
  }

  disconnect(): void {
    if (this.hubConnection) {
      this.hubConnection.stop().catch(() => {});
      this.hubConnection = undefined;
      this.connected.set(false);
    }
  }

  private appendHistory(m: RedisLiveMetrics): void {
    const time = new Date().toLocaleTimeString();
    const memMb = Math.round((m.memoryUsage / (1024 * 1024)) * 10) / 10;
    const ops = m.opsPerSecond || 0;
    const cpu = Math.round((m.cpuUsageInPercent || 0) * 10) / 10;
    const clients = m.connectedClients || 1;

    this.history.update(h => {
      const maxLen = 20;
      const timestamps = [...h.timestamps, time].slice(-maxLen);
      const memoryMb = [...h.memoryMb, memMb].slice(-maxLen);
      const opsPerSec = [...h.opsPerSec, ops].slice(-maxLen);
      const cpuPercent = [...h.cpuPercent, cpu].slice(-maxLen);
      const clientsList = [...h.clients, clients].slice(-maxLen);

      return {
        timestamps,
        memoryMb,
        opsPerSec,
        cpuPercent,
        clients: clientsList
      };
    });
  }
}
