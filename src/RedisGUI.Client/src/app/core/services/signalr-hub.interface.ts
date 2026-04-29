import { Observable } from 'rxjs';

/**
 * SignalR Hub Connection Interface
 * 
 * Provides abstraction for SignalR hub connections to improve testability
 * and allow for different implementations (real, mock, test).
 */
export interface ISignalRHubConnection {
  /**
   * Connection state signal
   */
  readonly connected: import('@angular/core').WritableSignal<boolean>;

  /**
   * Connect to the hub
   * @param queryParams Optional query parameters to include in the connection URL
   * @returns Observable that completes when connection is established
   */
  connect(queryParams?: Record<string, string>): Observable<void>;

  /**
   * Disconnect from the hub
   * @returns Observable that completes when disconnection is finished
   */
  disconnect(): Observable<void>;

  /**
   * Listen to messages from the hub
   * @returns Observable stream of messages
   * @template T - Type of messages emitted
   */
  onMessage<T = unknown>(): Observable<T>;

  /**
   * Invoke a hub method
   * @param methodName Name of the hub method to invoke
   * @param args Arguments to pass to the hub method
   * @returns Observable that emits the result
   * @template T - Return type of the hub method
   */
  invoke<T = void>(methodName: string, ...args: unknown[]): Observable<T>;
}

/**
 * SignalR Hub Service Factory Interface
 * 
 * Factory interface for creating SignalR hub connections.
 * Allows for dependency injection and testing.
 */
export interface ISignalRHubServiceFactory {
  /**
   * Create a new SignalR hub service instance
   * @param hubUrl URL of the hub
   * @param eventName Name of the event to listen for
   * @returns A new hub service instance
   */
  create<TMessage = unknown>(
    hubUrl: string,
    eventName: string
  ): ISignalRHubConnection;
}

