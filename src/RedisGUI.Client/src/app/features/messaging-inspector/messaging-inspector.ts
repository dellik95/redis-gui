import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe, JsonPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import * as signalR from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { ConnectionsStore } from '../../core/stores/connections.store';
import { TokenStorageService } from '../../core/services/auth/token-storage.service';
import { PageHeaderComponent } from '../../shared/components';
import { MessagingService, LiveMessage, MessagingMetrics, StreamEntry, StreamSnapshot } from './messaging.service';
import { chartPoints } from './messaging-metrics-chart';

@Component({
  selector: 'app-messaging-inspector',
  imports: [FormsModule, DatePipe, DecimalPipe, JsonPipe, MatIconModule, PageHeaderComponent],
  templateUrl: './messaging-inspector.html',
  styleUrl: './messaging-inspector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessagingInspector {
  private readonly route = inject(ActivatedRoute);
  private readonly messaging = inject(MessagingService);
  private readonly tokens = inject(TokenStorageService);
  readonly connections = inject(ConnectionsStore);
  readonly connectionId = this.route.snapshot.paramMap.get('id') ?? '';
  readonly connection = computed(() => this.connections.connections().find(c => c.id === this.connectionId));
  readonly canPublish = computed(() => !this.connection()?.isReadOnly && this.tokens.user()?.role !== 'ReadOnly');
  readonly mode = signal<'channel' | 'stream'>('channel');
  readonly status = signal('Stopped');
  readonly error = signal('');
  readonly messages = signal<LiveMessage[]>([]);
  readonly entries = signal<StreamEntry[]>([]);
  readonly snapshot = signal<StreamSnapshot | null>(null);
  readonly result = signal('');
  readonly channelInfo = signal<{ subscribers: number; patterns: number } | null>(null);
  readonly metrics = signal<MessagingMetrics[]>([]);
  readonly metricsError = signal('');
  readonly activeWatch = signal<{ mode: 'channel' | 'stream'; target: string; pattern: boolean } | null>(null);
  readonly latest = computed(() => this.metrics().at(-1));
  readonly ratePoints = computed(() => chartPoints(this.metrics().map(sample => sample.rate)));
  readonly countPoints = computed(() => chartPoints(this.metrics().flatMap(sample => sample.primary === null ? [] : [sample.primary])));
  readonly rateMax = computed(() => Math.max(1, ...this.metrics().map(sample => sample.rate)));
  readonly countMax = computed(() => Math.max(1, ...this.metrics().map(sample => sample.primary ?? 0)));
  channel = '';
  pattern = false;
  payload = '';
  streamKey = '';
  private hub: signalR.HubConnection | undefined;
  private generation = 0;
  private lastId = '0-0';
  private watch: { mode: 'channel' | 'stream'; target: string; pattern: boolean } | undefined;

  constructor() {
    this.connections.loadConnections();
    inject(DestroyRef).onDestroy(() => { void this.stop(); });
  }
  async start(): Promise<void> {
    const stopping = this.stop();
    const generation = this.generation;
    await stopping;
    if (generation !== this.generation) return;
    const mode = this.mode();
    const pattern = this.pattern;
    this.error.set('');
    this.metrics.set([]);
    this.metricsError.set('');
    const target = (mode === 'channel' ? this.channel : this.streamKey).trim();
    if (!target) { this.error.set('Enter a channel or stream key.'); return; }
    try {
      if (mode === 'stream') {
        const snapshot = await firstValueFrom(this.messaging.stream(this.connectionId, target));
        if (generation !== this.generation) return;
        this.snapshot.set(snapshot);
        this.entries.set(snapshot.entries);
        this.lastId = snapshot.lastGeneratedId;
      } else { this.messages.set([]); }
      this.watch = { mode, target, pattern };
      this.activeWatch.set(this.watch);
      const hub = new signalR.HubConnectionBuilder().withUrl('/hub/messaging', { accessTokenFactory: () => this.tokens.token() ?? '' }).withAutomaticReconnect().build();
      this.hub = hub;
      hub.on('Message', (message: LiveMessage) => { if (this.hub === hub) this.messages.update(items => [message, ...items].slice(0, 200)); });
      hub.on('StreamEntry', (entry: StreamEntry) => { if (this.hub === hub) { this.lastId = entry.id; this.entries.update(items => [entry, ...items].slice(0, 200)); } });
      hub.on('MonitorError', (message: string) => { if (this.hub === hub) { this.error.set(message); this.status.set('Stopped'); } });
      hub.on('MessagingMetrics', (sample: MessagingMetrics) => { if (this.hub === hub) { this.metricsError.set(''); this.metrics.update(items => [...items, sample].slice(-60)); } });
      hub.on('MetricsError', (message: string) => { if (this.hub === hub) this.metricsError.set(message); });
      hub.onreconnecting(() => { if (this.hub === hub) this.status.set('Reconnecting'); });
      hub.onreconnected(() => { if (this.hub === hub) { this.metrics.set([]); void this.subscribe().catch(() => { if (this.hub === hub) { this.status.set('Stopped'); this.error.set('Could not resume monitoring.'); } }); } });
      hub.onclose(() => { if (this.hub === hub) this.status.set('Stopped'); });
      await hub.start();
      if (this.hub !== hub) return;
      await this.subscribe();
    } catch { if (generation === this.generation) { this.error.set('Unable to start monitoring. Check the connection, permissions, and stream key.'); await this.stop(); } }
  }
  private async subscribe(): Promise<void> {
    const watch = this.watch;
    const hub = this.hub;
    if (!hub || !watch) return;
    if (watch.mode === 'channel') await hub.invoke('WatchChannel', this.connectionId, watch.target, watch.pattern);
    else await hub.invoke('WatchStream', this.connectionId, watch.target, this.lastId);
    if (this.hub === hub) this.status.set('Live');
  }
  async stop(): Promise<void> {
    this.generation++;
    const hub = this.hub;
    this.hub = undefined;
    this.watch = undefined;
    this.activeWatch.set(null);
    this.status.set('Stopped');
    if (hub) await hub.stop().catch(() => {});
  }
  async refreshChannelInfo(): Promise<void> {
    if (!this.channel.trim()) { this.error.set('Enter a channel.'); return; }
    try { this.channelInfo.set(await firstValueFrom(this.messaging.channelInfo(this.connectionId, this.channel.trim()))); }
    catch { this.error.set('Unable to load pub/sub information.'); }
  }
  async publish(): Promise<void> {
    this.error.set(''); this.result.set('');
    if (!this.channel.trim()) { this.error.set('Enter a channel to publish to.'); return; }
    try {
      const result = await firstValueFrom(this.messaging.publish(this.connectionId, this.channel.trim(), this.payload));
      this.result.set(`Published to ${result.subscribers} subscriber(s).`);
    } catch { this.error.set('Publish failed. Check the connection and write permissions.'); }
  }
}
