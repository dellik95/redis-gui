import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import * as signalR from '@microsoft/signalr';
import { SIGNALR_HUBS, SIGNALR_METHODS } from '../constants/api-routes.constants';

/**
 * Key change event interface
 */
export interface KeyChangeEvent {
  connectionId: string;
  key: string;
  type: string;
  action: 'created' | 'updated' | 'deleted';
  timestamp: string;
}

/**
 * SignalR state interface
 */
interface SignalRState {
  /**
   * Key change events
   */
  keyChangeEvents: KeyChangeEvent[];

  /**
   * Key change hub connection status
   */
  isKeyChangeConnected: boolean;

  /**
   * Watched connection IDs for key changes
   */
  watchedConnections: string[];

  /**
   * Error message
   */
  error: string | null;
}

/**
 * Initial state
 */
const initialState: SignalRState = {
  keyChangeEvents: [],
  isKeyChangeConnected: false,
  watchedConnections: [],
  error: null
};

/**
 * Global SignalR store using NgRx SignalStore
 * Manages real-time SignalR connection for Key Change events
 */
export const SignalRStore = signalStore(
  { providedIn: 'root' },

  // Define state
  withState(initialState),

  // Define computed signals
  withComputed(store => ({
    /**
     * Total number of key change events
     */
    keyChangeEventCount: computed(() => store.keyChangeEvents().length),

    /**
     * Latest key change event
     */
    latestKeyChangeEvent: computed(() => {
      const events = store.keyChangeEvents();
      return events.length > 0 ? events[events.length - 1] : null;
    }),

    /**
     * Whether key change hub is connected
     */
    isConnected: computed(() => store.isKeyChangeConnected()),

    /**
     * Number of watched connections
     */
    watchedConnectionCount: computed(() => store.watchedConnections().length)
  })),

  // Define methods
  withMethods(store => {
    let keyChangeConnection: signalR.HubConnection | undefined;

    const createConnection = (
      hubUrl: string,
      queryParams?: Record<string, string>
    ): signalR.HubConnection => {
      let url = hubUrl;
      if (queryParams) {
        const params = new URLSearchParams(queryParams);
        url = `${hubUrl}?${params.toString()}`;
      }

      return new signalR.HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.previousRetryCount === 0) return 0;
            if (retryContext.previousRetryCount < 3) return retryContext.previousRetryCount * 2000;
            if (retryContext.previousRetryCount < 5) return 10000;
            return 30000;
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();
    };

    return {
      /**
       * Connect to Key Change notification hub
       * @param connectionId Optional Redis connection ID to watch immediately
       */
      connectKeyChange(connectionId?: string): void {
        if (keyChangeConnection) return;

        const queryParams = connectionId ? { connectionId } : undefined;
        keyChangeConnection = createConnection(SIGNALR_HUBS.KEY_CHANGE, queryParams);

        keyChangeConnection.on(SIGNALR_METHODS.KEY_CHANGE.KEY_CHANGED, (evt: KeyChangeEvent) => {
          const events = [...store.keyChangeEvents(), evt];
          patchState(store, { keyChangeEvents: events });
        });

        keyChangeConnection.onreconnecting(() => {
          patchState(store, { isKeyChangeConnected: false });
        });

        keyChangeConnection.onreconnected(() => {
          patchState(store, { isKeyChangeConnected: true });
        });

        keyChangeConnection.onclose(() => {
          patchState(store, { isKeyChangeConnected: false });
        });

        keyChangeConnection
          .start()
          .then(() => {
            patchState(store, {
              isKeyChangeConnected: true,
              error: null
            });
            if (connectionId) {
              patchState(store, {
                watchedConnections: [...store.watchedConnections(), connectionId]
              });
            }
          })
          .catch(err => {
            patchState(store, {
              isKeyChangeConnected: false,
              error: err.message || 'Failed to connect to key change hub'
            });
          });
      },

      /**
       * Watch a Redis connection for key changes
       * @param redisConnectionId The Redis connection ID to watch
       */
      async watchConnection(redisConnectionId: string): Promise<void> {
        if (
          !keyChangeConnection ||
          keyChangeConnection.state !== signalR.HubConnectionState.Connected
        ) {
          return;
        }

        try {
          await keyChangeConnection.invoke(
            SIGNALR_METHODS.KEY_CHANGE.WATCH_CONNECTION,
            redisConnectionId
          );
          patchState(store, {
            watchedConnections: [...store.watchedConnections(), redisConnectionId]
          });
        } catch (err) {
          patchState(store, {
            error: (err as Error).message || 'Failed to watch connection'
          });
        }
      },

      /**
       * Stop watching a Redis connection for key changes
       * @param redisConnectionId The Redis connection ID to unwatch
       */
      async unwatchConnection(redisConnectionId: string): Promise<void> {
        if (
          !keyChangeConnection ||
          keyChangeConnection.state !== signalR.HubConnectionState.Connected
        ) {
          return;
        }

        try {
          await keyChangeConnection.invoke(
            SIGNALR_METHODS.KEY_CHANGE.UNWATCH_CONNECTION,
            redisConnectionId
          );
          patchState(store, {
            watchedConnections: store.watchedConnections().filter(id => id !== redisConnectionId)
          });
        } catch (err) {
          patchState(store, {
            error: (err as Error).message || 'Failed to unwatch connection'
          });
        }
      },

      /**
       * Disconnect from Key Change hub
       */
      disconnectKeyChange(): void {
        if (!keyChangeConnection) return;

        keyChangeConnection
          .stop()
          .then(() => {
            keyChangeConnection = undefined;
            patchState(store, {
              isKeyChangeConnected: false,
              watchedConnections: []
            });
          })
          .catch(err => {
            patchState(store, {
              error: err.message || 'Failed to disconnect from key change hub'
            });
          });
      },

      /**
       * Disconnect from all hubs
       */
      disconnectAll(): void {
        if (keyChangeConnection) {
          keyChangeConnection.stop().catch(() => {});
          keyChangeConnection = undefined;
        }

        patchState(store, {
          isKeyChangeConnected: false,
          watchedConnections: []
        });
      },

      /**
       * Clear all key change events
       */
      clearKeyChangeEvents(): void {
        patchState(store, { keyChangeEvents: [] });
      },

      /**
       * Clear error state
       */
      clearError(): void {
        patchState(store, { error: null });
      },

      /**
       * Reset store to initial state
       */
      reset(): void {
        if (keyChangeConnection) {
          keyChangeConnection.stop().catch(() => {});
          keyChangeConnection = undefined;
        }

        patchState(store, initialState);
      }
    };
  })
);
