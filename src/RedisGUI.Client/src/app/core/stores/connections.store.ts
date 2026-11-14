import { computed, inject } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, tap, catchError, of, switchMap, Subject, takeUntil } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import {
  CreateConnectionRequest,
  UpdateConnectionRequest,
  CheckConnectionRequest
} from '../../models/connection-request.types';
import { Connection } from '../../models/connection.type';
import { API_ROUTES } from '../constants/api-routes.constants';

/**
 * Connections state interface
 */
interface ConnectionsState {
  /**
   * List of all connections
   */
  connections: Connection[];

  /**
   * Currently selected connection
   */
  selectedConnection: Connection | null;

  /**
   * Loading state
   */
  isLoading: boolean;

  /**
   * Error message
   */
  error: string | null;

  /**
   * Last fetch timestamp
   */
  lastFetched: number | null;

  /**
   * Last update timestamp (for notifying subscribers of changes)
   * Replaces Subject-based notification pattern
   */
  lastUpdateTimestamp: number;
}

/**
 * Initial connections state
 */
const initialState: ConnectionsState = {
  connections: [],
  selectedConnection: null,
  isLoading: false,
  error: null,
  lastFetched: null,
  lastUpdateTimestamp: 0
};

/**
 * Global connections store using NgRx SignalStore
 * Replaces manual caching with centralized state management
 *
 * @example
 * ```typescript
 * // Inject the store
 * private connectionsStore = inject(ConnectionsStore);
 *
 * // Load connections
 * this.connectionsStore.loadConnections();
 *
 * // Read state
 * const connections = this.connectionsStore.connections();
 * const count = this.connectionsStore.connectionsCount();
 *
 * // Create/update/delete
 * this.connectionsStore.createConnection(request);
 * ```
 */
export const ConnectionsStore = signalStore(
  { providedIn: 'root' },

  // Define state
  withState(initialState),

  // Define computed signals
  withComputed(store => ({
    /**
     * Number of connections
     */
    connectionsCount: computed(() => store.connections().length),

    /**
     * Whether connections list is empty
     */
    isEmpty: computed(() => store.connections().length === 0)
  })),

  // Define methods
  withMethods((store, httpClient = inject(HttpClient)) => {
    const sessionEnded = new Subject<void>();
    return {
      resetSession(): void {
        sessionEnded.next();
        patchState(store, initialState);
      },
      needsRefresh(): boolean {
        const fetched = store.lastFetched();
        return !fetched || Date.now() - fetched > 30_000;
      },
      /**
       * Loads all connections from the API
       */
      loadConnections: rxMethod<void>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(() =>
            httpClient.get<Connection[]>(API_ROUTES.CONNECTIONS.LIST).pipe(
              takeUntil(sessionEnded),
              tap(connections =>
                patchState(store, {
                  connections,
                  isLoading: false,
                  lastFetched: Date.now()
                })
              ),
              catchError(error => {
                patchState(store, {
                  error: error.message || 'Failed to load connections',
                  isLoading: false
                });
                return of([]);
              })
            )
          )
        )
      ),

      /**
       * Creates a new connection
       */
      createConnection: rxMethod<CreateConnectionRequest>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(request =>
            httpClient.post<Connection>(API_ROUTES.CONNECTIONS.CREATE, request).pipe(
              takeUntil(sessionEnded),
              tap(newConnection => {
                // Add new connection to state
                patchState(store, {
                  connections: [...store.connections(), newConnection],
                  isLoading: false,
                  lastFetched: Date.now()
                });
              }),
              catchError(error => {
                patchState(store, {
                  error: error.message || 'Failed to create connection',
                  isLoading: false
                });
                return of(null);
              })
            )
          )
        )
      ),

      /**
       * Updates an existing connection
       */
      updateConnection: rxMethod<UpdateConnectionRequest>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(request =>
            httpClient.put<void>(API_ROUTES.CONNECTIONS.UPDATE(request.id), request).pipe(
              takeUntil(sessionEnded),
              tap(() => {
                // Update connection in state
                const updatedConnections = store
                  .connections()
                  .map(conn => (conn.id === request.id ? { ...conn, ...request } : conn));
                patchState(store, {
                  connections: updatedConnections,
                  isLoading: false,
                  lastFetched: Date.now()
                });
              }),
              catchError(error => {
                patchState(store, {
                  error: error.message || 'Failed to update connection',
                  isLoading: false
                });
                return of(null);
              })
            )
          )
        )
      ),

      /**
       * Deletes a connection
       */
      deleteConnection: rxMethod<string>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(id =>
            httpClient.delete<void>(API_ROUTES.CONNECTIONS.DELETE(id)).pipe(
              takeUntil(sessionEnded),
              tap(() => {
                // Remove connection from state
                const updatedConnections = store.connections().filter(conn => conn.id !== id);
                patchState(store, {
                  connections: updatedConnections,
                  selectedConnection:
                    store.selectedConnection()?.id === id ? null : store.selectedConnection(),
                  isLoading: false,
                  lastFetched: Date.now()
                });
              }),
              catchError(error => {
                patchState(store, {
                  error: error.message || 'Failed to delete connection',
                  isLoading: false
                });
                return of(null);
              })
            )
          )
        )
      ),

      /**
       * Checks if a connection is valid
       */
      checkConnection: rxMethod<CheckConnectionRequest>(
        pipe(
          switchMap(request =>
            httpClient.post<void>(API_ROUTES.CONNECTIONS.CHECK, request).pipe(
              takeUntil(sessionEnded),
              catchError(error => {
                return of({ success: false, error: error.message });
              })
            )
          )
        )
      ),

      /**
       * Loads a single connection by ID from the API
       */
      loadConnectionById: rxMethod<string>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(id =>
            httpClient.get<Connection>(API_ROUTES.CONNECTIONS.GET(id)).pipe(
              takeUntil(sessionEnded),
              tap(connection => {
                // Update connection in state if it exists, otherwise add it
                const existingConnections = store.connections();
                const index = existingConnections.findIndex(c => c.id === id);

                if (index >= 0) {
                  const updatedConnections = [...existingConnections];
                  updatedConnections[index] = connection;
                  patchState(store, {
                    connections: updatedConnections,
                    isLoading: false
                  });
                } else {
                  patchState(store, {
                    connections: [...existingConnections, connection],
                    isLoading: false
                  });
                }
              }),
              catchError(error => {
                patchState(store, {
                  error: error.message || 'Failed to load connection',
                  isLoading: false
                });
                return of(null);
              })
            )
          )
        )
      ),

      /**
       * Selects a connection
       */
      selectConnection(connection: Connection | null): void {
        patchState(store, { selectedConnection: connection });
      },

      /**
       * Gets connection by ID from local state (synchronous)
       */
      getConnectionById(id: string): Connection | undefined {
        return store.connections().find(conn => conn.id === id);
      },

      /**
       * Clears error state
       */
      clearError(): void {
        patchState(store, { error: null });
      },

      /**
       * Forces a refresh of connections
       */
      refresh(): void {
        patchState(store, { lastFetched: null });
      },

      /**
       * Notifies that connections were updated
       * Replaces Subject-based notification pattern
       */
      notifyUpdate(): void {
        patchState(store, { lastUpdateTimestamp: Date.now() });
      }
    };
  })
);
