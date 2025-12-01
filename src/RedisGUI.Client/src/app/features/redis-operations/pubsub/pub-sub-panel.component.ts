import { Component, inject, signal, effect, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInput, MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule, MatChipListbox, MatChip } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RedisOperationsService } from '../../../services/api/redis-operations.service';
import { SignalRRealtimeService } from '../../../shared/services/signalr-realtime.service';
import { SubscribeToChannelResponse } from '../../../models/redis-advanced.types';

interface PubSubMessage {
  timestamp: Date;
  channel: string;
  message: string;
  outgoing: boolean; // true if published locally
}

@Component({
  selector: 'app-pub-sub-panel',
  standalone: true,
  templateUrl: './pub-sub-panel.component.html',
  styleUrls: ['./pub-sub-panel.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ScrollingModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatChipsModule,
    MatChipListbox,
    MatChip,
    MatTooltipModule,
	MatInput
  ]
})
export class PubSubPanelComponent {
    updateSubscribeChannel(value: string): void {
      this.subscribeChannel.set(value);
    }

    updatePublishChannel(value: string): void {
      this.publishChannel.set(value);
    }

    updatePublishMessage(value: string): void {
      this.publishMessage.set(value);
    }
  private redisOperationsService = inject(RedisOperationsService);
  private snackBar = inject(MatSnackBar);
  private signalr = inject(SignalRRealtimeService);

  // Required connection id passed from parent
  connectionId = input.required<string>();

  // Signals for form fields
  subscribeChannel = signal<string>('');
  publishChannel = signal<string>('');
  publishMessage = signal<string>('');
  autoScroll = signal<boolean>(true);

  // State
  subscriptions = signal<SubscribeToChannelResponse[]>([]);
  messages = signal<PubSubMessage[]>([]);

  // Real-time server push via SignalR
  constructor() {
    this.signalr.connectPubSub();
    effect(() => {
      const incoming = this.signalr.pubSubMessages();
      if (incoming.length > 0) {
        // Add incoming messages to local list
        for (const msg of incoming) {
          this.messages.update(list => [...list, {
            timestamp: new Date(msg.timestamp),
            channel: msg.channel,
            message: msg.message,
            outgoing: false
          }]);
        }
        this.signalr.clearPubSub();
        if (this.autoScroll()) setTimeout(() => this.scrollToBottom(), 50);
      }
    });
  }

  get isSubscribedToPublishChannel(): boolean {
    const channel = this.publishChannel();
    return !!this.subscriptions().find(s => s.channel === channel);
  }

  subscribe(): void {
    const channel = this.subscribeChannel().trim();
    if (!channel) {
      this.snackBar.open('Channel required', 'Close', { duration: 3000 });
      return;
    }
    if (this.subscriptions().some(s => s.channel === channel)) {
      this.snackBar.open('Already subscribed', 'Close', { duration: 3000 });
      return;
    }
    this.redisOperationsService.subscribeToChannel(this.connectionId(), channel).subscribe({
      next: resp => {
        if (resp.success) {
          this.subscriptions.update(list => [...list, resp]);
          this.snackBar.open(`Subscribed to ${channel}`, 'Close', { duration: 3000 });
          this.subscribeChannel.set('');
        } else {
          this.snackBar.open(`Failed to subscribe ${channel}`, 'Close', { duration: 4000 });
        }
      },
      error: err => {
        this.snackBar.open(`Error subscribing: ${err.message}`, 'Close', { duration: 4000 });
      }
    });
  }

  unsubscribe(subscriptionId: string): void {
    this.redisOperationsService.unsubscribeFromChannel(this.connectionId(), subscriptionId).subscribe({
      next: () => {
        this.subscriptions.update(list => list.filter(s => s.subscriptionId !== subscriptionId));
        this.snackBar.open('Unsubscribed', 'Close', { duration: 3000 });
      },
      error: err => this.snackBar.open(`Error unsubscribing: ${err.message}`, 'Close', { duration: 4000 })
    });
  }

  publish(): void {
    const channel = this.publishChannel().trim();
    const message = this.publishMessage().trim();
    if (!channel || !message) {
      this.snackBar.open('Channel and message required', 'Close', { duration: 3000 });
      return;
    }
    this.redisOperationsService.publishMessage(this.connectionId(), { channel, message }).subscribe({
      next: () => {
        // Add message locally
        this.messages.update(list => [...list, { timestamp: new Date(), channel, message, outgoing: true }]);
        this.publishMessage.set('');
        if (this.autoScroll()) {
          setTimeout(() => this.scrollToBottom(), 50);
        }
      },
      error: err => this.snackBar.open(`Publish error: ${err.message}`, 'Close', { duration: 4000 })
    });
  }

  clearMessages(): void {
    this.messages.set([]);
  }

  trackByMsg(_index: number, msg: PubSubMessage): string {
    return msg.timestamp.getTime().toString() + msg.channel + msg.message;
  }

  private scrollToBottom(): void {
    const container = document.querySelector('.messages-viewport');
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  }
}
