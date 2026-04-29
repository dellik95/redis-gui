import { Injectable, signal, WritableSignal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { ISignalRHubConnection } from './signalr-hub.interface';

/**
 * Base SignalR Hub Service
 *
 * Provides common functionality for SignalR hub connections.
 * Implements ISignalRHubConnection for abstraction and testability.
 *
 * Features:
 * - Connection lifecycle management
 * - Automatic reconnection with exponential backoff
 * - Message streaming via Observables
 * - Hub method invocation
 * - Connection state signals
 *
 * @template TMessage - Type of messages received from the hub
 *
 * @example
 * ```typescript
 * @Injectable({ providedIn: 'root' })
 * export class MyHubService extends SignalRHubService<MyMessage> {
 *   protected override hubUrl = '/hub/myhub';
 *   protected override eventName = 'ReceiveMessage';
 * }
 *
 * // Usage
 * private hub = inject(MyHubService);
 *
 * // Connect
 * this.hub.connect().subscribe(() => {
 *   // Connected
 * });
 *
 * // Listen to messages
 * this.hub.onMessage().subscribe(message => {
 *   console.log('Received:', message);
 * });
 * ```
 */
@Injectable()
export abstract class SignalRHubService<TMessage = unknown> implements ISignalRHubConnection {
  // 1. Protected state
  /** SignalR connection instance */
  protected connection?: signalR.HubConnection;

  /** Connection state signal */
  public readonly connected: WritableSignal<boolean> = signal(false);

  // 2. Abstract properties (must be implemented by derived classes)
  /** Hub URL endpoint */
  protected abstract hubUrl: string;

  /** Event name for receiving messages */
  protected abstract eventName: string;

  /**
   * Create and configure the SignalR connection
   * Override this method to customize connection setup
   */
  protected createConnection(queryParams?: Record<string, string>): signalR.HubConnection {
    const url = queryParams
      ? `${this.hubUrl}?${new URLSearchParams(queryParams).toString()}`
      : this.hubUrl;

    return new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, then every 30s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount < 3) return retryContext.previousRetryCount * 2000;
          if (retryContext.previousRetryCount < 5) return 10000;
          return 30000;
        }
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }

  /**
   * Start the SignalR connection
   */
  connect(queryParams?: Record<string, string>): Observable<void> {
    if (this.connection) {
      return new Observable<void>(observer => {
        observer.next();
        observer.complete();
      });
    }

    this.connection = this.createConnection(queryParams);
    this.setupConnectionHandlers();
    this.registerEventHandlers(this.connection);

    return new Observable<void>(observer => {
      this.connection!.start()
        .then(() => {
          this.connected.set(true);
          this.onConnected();
          observer.next();
          observer.complete();
        })
        .catch(error => {
          this.connected.set(false);
          this.onConnectionError(error);
          observer.error(error);
        });
    });
  }

  /**
   * Stop the SignalR connection
   */
  disconnect(): Observable<void> {
    if (!this.connection) {
      return new Observable<void>(observer => {
        observer.next();
        observer.complete();
      });
    }

    return new Observable<void>(observer => {
      this.connection!.stop()
        .then(() => {
          this.connected.set(false);
          delete this.connection;
          this.onDisconnected();
          observer.next();
          observer.complete();
        })
        .catch(error => {
          observer.error(error);
        });
    });
  }

  /**
   * Listen to messages from the hub
   */
  onMessage<T = TMessage>(): Observable<T> {
    return new Observable<T>(observer => {
      if (!this.connection) {
        observer.error(new Error('Connection not established'));
        return;
      }

      this.connection.on(this.eventName, (message: TMessage) => {
        observer.next(message as unknown as T);
      });

      // Cleanup on unsubscribe
      return () => {
        if (this.connection) {
          this.connection.off(this.eventName);
        }
      };
    });
  }

  /**
   * Setup connection lifecycle handlers
   */
  private setupConnectionHandlers(): void {
    if (!this.connection) return;

    this.connection.onreconnecting(() => {
      this.connected.set(false);
      this.onReconnecting();
    });

    this.connection.onreconnected(() => {
      this.connected.set(true);
      this.onReconnected();
    });

    this.connection.onclose(() => {
      this.connected.set(false);
      this.onConnectionClosed();
    });
  }

  /**
   * Register custom event handlers
   * Override this to register multiple event handlers
   */
  protected registerEventHandlers(connection: signalR.HubConnection): void {
    // Base implementation - override in derived classes if needed
  }

  /**
   * Invoke a hub method
   *
   * @param methodName - Name of the hub method to invoke
   * @param args - Arguments to pass to the hub method
   * @returns Observable that emits the method result
   * @template T - Return type of the hub method
   */
  public invoke<T = void>(methodName: string, ...args: unknown[]): Observable<T> {
    return new Observable<T>(observer => {
      if (!this.connection) {
        observer.error(new Error('Connection not established'));
        return;
      }

      this.connection.invoke<T>(methodName, ...args)
        .then(result => {
          observer.next(result);
          observer.complete();
        })
        .catch(error => {
          observer.error(error);
        });
    });
  }

  // 3. Lifecycle hooks (override in derived classes if needed)
  /**
   * Called when connection is established
   * Override in derived classes for custom behavior
   */
  protected onConnected(): void {
    // Override in derived classes if needed
  }

  /**
   * Called when connection is disconnected
   * Override in derived classes for custom behavior
   */
  protected onDisconnected(): void {
    // Override in derived classes if needed
  }

  /**
   * Called when connection is reconnecting
   * Override in derived classes for custom behavior
   */
  protected onReconnecting(): void {
    // Override in derived classes if needed
  }

  /**
   * Called when connection is reconnected
   * Override in derived classes for custom behavior
   */
  protected onReconnected(): void {
    // Override in derived classes if needed
  }

  /**
   * Called when connection is closed
   * Override in derived classes for custom behavior
   */
  protected onConnectionClosed(): void {
    // Override in derived classes if needed
  }

  /**
   * Handles connection errors
   * Override in derived classes for custom error handling
   *
   * @param error - The connection error
   */
  protected onConnectionError(error: unknown): void {
    // Silent error handling - override in derived classes if logging is needed
    // Error is already propagated to Observable subscribers
  }
}
