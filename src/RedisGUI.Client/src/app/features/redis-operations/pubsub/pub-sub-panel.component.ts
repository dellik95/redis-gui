import { Component, inject, signal, effect, input, computed, viewChild, ElementRef, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInput, MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule, MatChipListbox, MatChip } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ScrollingModule } from '@angular/cdk/scrolling';

import { RedisOperationsStore } from '../../../core/stores/redis-operations.store';
import { SignalRStore } from '../../../core/stores/signalr.store';
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
  styleUrl: './pub-sub-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
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
  // 1. Injected dependencies
  private readonly redisOperationsStore = inject(RedisOperationsStore);
  private readonly snackBar = inject(MatSnackBar);
  private readonly signalRStore = inject(SignalRStore);
  private readonly destroyRef = inject(DestroyRef);

  // 2. Inputs
  readonly connectionId = input.required<string>();

  // 3. View queries
  readonly messagesViewport = viewChild<ElementRef>('messagesViewport');

  // 4. State signals
  readonly subscribeChannel = signal<string>('');
  readonly publishChannel = signal<string>('');
  readonly publishMessage = signal<string>('');
  readonly autoScroll = signal<boolean>(true);
  readonly subscriptions = signal<SubscribeToChannelResponse[]>([]);
  readonly messages = signal<PubSubMessage[]>([]);

  // 5. Computed signals
  readonly isSubscribedToPublishChannel = computed(() => {
    const channel = this.publishChannel();
    return this.subscriptions().some(s => s.channel === channel);
  });

  readonly canSubscribe = computed(() => {
    const channel = this.subscribeChannel().trim();
    return channel.length > 0 && !this.subscriptions().some(s => s.channel === channel);
  });

  readonly canPublish = computed(() => {
    const channel = this.publishChannel().trim();
    const message = this.publishMessage().trim();
    return channel.length > 0 && message.length > 0;
  });

  // 6. Constructor with effects
  constructor() {
    this.signalRStore.connectPubSub();
    
    // Handle incoming messages from SignalR
    effect(() => {
      const incoming = this.signalRStore.pubSubMessages();
      if (incoming.length > 0) {
        // Add incoming messages to local list
        const newMessages: PubSubMessage[] = incoming.map(msg => ({
          timestamp: new Date(msg.timestamp),
          channel: msg.channel,
          message: msg.message,
          outgoing: false
        }));
        
        this.messages.update(list => [...list, ...newMessages]);
        this.signalRStore.clearPubSubMessages();
        
        // Scroll to bottom after messages are added
        if (this.autoScroll()) {
          requestAnimationFrame(() => this.scrollToBottom());
        }
      }
    });

    // Watch for subscription changes from store
    effect(() => {
      const activeSubscriptions = this.redisOperationsStore.activeSubscriptions();
      // Update local subscriptions list when store changes
      this.subscriptions.set(activeSubscriptions);
    });
  }

  /**
   * Subscribe to a channel
   */
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
    
    this.redisOperationsStore.subscribeToChannel({
      connectionId: this.connectionId(),
      channel
    });
    
    // Clear input field
    this.subscribeChannel.set('');
    
    // Success/error handling is done via effect watching activeSubscriptions
  }

  /**
   * Unsubscribe from a channel
   */
  unsubscribe(subscriptionId: string): void {
    this.redisOperationsStore.unsubscribeFromChannel({
      connectionId: this.connectionId(),
      subscriptionId
    });
    
    // Subscription removal is handled via effect watching activeSubscriptions
  }

  /**
   * Publish a message to a channel
   */
  publish(): void {
    const channel = this.publishChannel().trim();
    const message = this.publishMessage().trim();
    if (!channel || !message) {
      this.snackBar.open('Channel and message required', 'Close', { duration: 3000 });
      return;
    }
    
    this.redisOperationsStore.publishMessage({
      connectionId: this.connectionId(),
      request: { channel, message }
    });
    
    // Add message locally immediately
    this.messages.update(list => [...list, { 
      timestamp: new Date(), 
      channel, 
      message, 
      outgoing: true 
    }]);
    this.publishMessage.set('');
    
    // Scroll to bottom after message is added
    if (this.autoScroll()) {
      requestAnimationFrame(() => this.scrollToBottom());
    }
  }

  // Note: Channel and message signals are updated directly via two-way binding in template
  // No need for separate update methods

  /**
   * Clear all messages
   */
  clearMessages(): void {
    this.messages.set([]);
  }

  /**
   * Track by function for message list
   */
  trackByMsg(_index: number, msg: PubSubMessage): string {
    return `${msg.timestamp.getTime()}-${msg.channel}-${msg.message}`;
  }

  /**
   * Scroll messages viewport to bottom
   */
  private scrollToBottom(): void {
    const viewport = this.messagesViewport()?.nativeElement;
    if (viewport) {
      viewport.scrollTop = viewport.scrollHeight;
    }
  }
}
