import { Injectable, signal, WritableSignal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { SignalRHubService } from '../../core/services/signalr-hub.service';

export interface PubSubMessage {
  connectionId: string;
  channel: string;
  message: string;
  timestamp: string;
}

export interface KeyChangeEvent {
  connectionId: string;
  key: string;
  type: string;
  action: 'created' | 'updated' | 'deleted';
  timestamp: string;
}

/**
 * Real-time SignalR service for Pub/Sub messages and key change notifications
 * Extends base SignalRHubService for connection management
 */
@Injectable({ providedIn: 'root' })
export class SignalRRealtimeService extends SignalRHubService<PubSubMessage> {
  protected override hubUrl = '/hub/pubsub';
  protected override eventName = 'ReceiveMessage';

  // Message storage
  pubSubMessages: WritableSignal<PubSubMessage[]> = signal<PubSubMessage[]>([]);
  keyChangeEvents: WritableSignal<KeyChangeEvent[]> = signal<KeyChangeEvent[]>([]);

  // Key change hub connection (separate from main connection)
  private keyChangeConnection?: signalR.HubConnection;
  private keyChangeHubUrl = '/hub/keychange';

  /**
   * Connect to Pub/Sub hub
   */
  connectPubSub(): void {
    if (this.connection) return;
    
    this.connect().subscribe({
      next: () => console.log('PubSub hub connected'),
      error: (err) => console.error('PubSub hub connection failed:', err)
    });
  }

  /**
   * Connect to Key Change notification hub
   */
  connectKeyChange(): void {
    if (this.keyChangeConnection) return;

    this.keyChangeConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.keyChangeHubUrl)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount < 3) return retryContext.previousRetryCount * 2000;
          if (retryContext.previousRetryCount < 5) return 10000;
          return 30000;
        }
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Register key change event handler
    this.keyChangeConnection.on('KeyChanged', (evt: KeyChangeEvent) => {
      this.keyChangeEvents.update(list => [...list, evt]);
    });

    // Setup lifecycle handlers
    this.keyChangeConnection.onreconnecting(() => {
      console.log('Key change hub reconnecting...');
    });

    this.keyChangeConnection.onreconnected(() => {
      console.log('Key change hub reconnected');
    });

    this.keyChangeConnection.onclose(() => {
      console.log('Key change hub connection closed');
    });

    // Start connection
    this.keyChangeConnection.start()
      .then(() => console.log('Key change hub connected'))
      .catch(err => console.error('Key change hub connection failed:', err));
  }

	/**
	 * Disconnect from all hubs
	 */
	disconnectAll(): void {
		// Disconnect main pub/sub hub
		if (this.connection) {
			super.disconnect().subscribe();
		}

		// Disconnect key change hub
		if (this.keyChangeConnection) {
			this.keyChangeConnection.stop()
				.then(() => {
					this.keyChangeConnection = undefined;
					console.log('Key change hub disconnected');
				})
				.catch(err => console.error('Error disconnecting key change hub:', err));
		}
	}  /**
   * Register event handlers for the main hub
   */
  protected override registerEventHandlers(connection: signalR.HubConnection): void {
    connection.on(this.eventName, (msg: PubSubMessage) => {
      this.pubSubMessages.update(list => [...list, msg]);
    });
  }

  /**
   * Clear all Pub/Sub messages
   */
  clearPubSub(): void {
    this.pubSubMessages.set([]);
  }

  /**
   * Clear all key change events
   */
  clearKeyChanges(): void {
    this.keyChangeEvents.set([]);
  }

  /**
   * Lifecycle hooks
   */
  protected override onConnected(): void {
    console.log('SignalR PubSub hub connected successfully');
  }

  protected override onDisconnected(): void {
    console.log('SignalR PubSub hub disconnected');
  }

  protected override onReconnecting(): void {
    console.log('SignalR PubSub hub reconnecting...');
  }

  protected override onReconnected(): void {
    console.log('SignalR PubSub hub reconnected successfully');
  }

  protected override onConnectionError(error: any): void {
    console.error('SignalR PubSub hub connection error:', error);
  }
}
