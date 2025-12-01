import { Injectable, signal, WritableSignal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';

/**
 * Base SignalR Hub Service
 * Provides common functionality for SignalR hub connections
 */
@Injectable()
export abstract class SignalRHubService<TMessage = any> {
  protected connection?: signalR.HubConnection;
  public connected: WritableSignal<boolean> = signal(false);
  
  protected abstract hubUrl: string;
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
          this.connection = undefined;
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
  protected onMessage(): Observable<TMessage> {
    return new Observable<TMessage>(observer => {
      if (!this.connection) {
        observer.error(new Error('Connection not established'));
        return;
      }

      this.connection.on(this.eventName, (message: TMessage) => {
        observer.next(message);
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
   */
  protected invoke<T = void>(methodName: string, ...args: any[]): Observable<T> {
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

  /**
   * Lifecycle hooks - override in derived classes
   */
  protected onConnected(): void {
    console.log(`SignalR connected to ${this.hubUrl}`);
  }

  protected onDisconnected(): void {
    console.log(`SignalR disconnected from ${this.hubUrl}`);
  }

  protected onReconnecting(): void {
    console.log(`SignalR reconnecting to ${this.hubUrl}...`);
  }

  protected onReconnected(): void {
    console.log(`SignalR reconnected to ${this.hubUrl}`);
  }

  protected onConnectionClosed(): void {
    console.log(`SignalR connection closed to ${this.hubUrl}`);
  }

  protected onConnectionError(error: any): void {
    console.error(`SignalR connection error to ${this.hubUrl}:`, error);
  }
}
