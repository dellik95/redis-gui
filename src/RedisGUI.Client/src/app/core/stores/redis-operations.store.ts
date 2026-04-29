import { computed, inject } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, tap, catchError, of, switchMap } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
	PagedRedisValuesResponse,
	DeleteKeysByPatternResponse,
	DeleteMultipleKeysResponse,
	ExportRedisDataResponse,
	ImportRedisDataRequest,
	ImportRedisDataResponse,
	PublishMessageRequest,
	SubscribeToChannelResponse,
	ExecuteTransactionRequest,
	ExecuteTransactionResponse,
	ExecuteLuaScriptRequest,
	ExecuteLuaScriptResponse,
	GetConnectionHealthResponse
} from '../../models/redis-advanced.types';

/**
 * Redis operations state interface
 */
interface RedisOperationsState {
	/**
	 * Paginated values response
	 */
	pagedValues: PagedRedisValuesResponse | null;

	/**
	 * Connection health data
	 */
	connectionHealth: GetConnectionHealthResponse | null;

	/**
	 * Last transaction result
	 */
	lastTransactionResult: ExecuteTransactionResponse | null;

	/**
	 * Last Lua script result
	 */
	lastScriptResult: ExecuteLuaScriptResponse | null;

	/**
	 * Active subscriptions
	 */
	activeSubscriptions: SubscribeToChannelResponse[];

	/**
	 * Loading state
	 */
	isLoading: boolean;

	/**
	 * Error message
	 */
	error: string | null;
}

/**
 * Initial state
 */
const initialState: RedisOperationsState = {
	pagedValues: null,
	connectionHealth: null,
	lastTransactionResult: null,
	lastScriptResult: null,
	activeSubscriptions: [],
	isLoading: false,
	error: null
};

/**
 * Global Redis operations store using NgRx SignalStore
 * Provides centralized state management for Redis advanced operations
 * 
 * Features:
 * - Pagination
 * - Bulk delete operations
 * - Import/Export
 * - Pub/Sub
 * - Transactions
 * - Lua Scripts
 * - Health monitoring
 * 
 * @example
 * ```typescript
 * // Inject the store
 * private redisOpsStore = inject(RedisOperationsStore);
 * 
 * // Load paged values
 * this.redisOpsStore.loadPagedValues({
 *   connectionId: 'conn-123',
 *   pattern: 'user:*',
 *   pageNumber: 1,
 *   pageSize: 50
 * });
 * 
 * // Execute transaction
 * this.redisOpsStore.executeTransaction({
 *   connectionId: 'conn-123',
 *   request: { commands: ['SET key1 val1', 'GET key1'] }
 * });
 * 
 * // Check health
 * this.redisOpsStore.loadConnectionHealth({ connectionId: 'conn-123' });
 * ```
 */
export const RedisOperationsStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Whether connection is healthy
		 */
		isHealthy: computed(() => {
			const health = store.connectionHealth();
			return health?.status === 'Healthy' || health?.status === 'Connected';
		}),

		/**
		 * Number of active subscriptions
		 */
		subscriptionsCount: computed(() => {
			return store.activeSubscriptions().length;
		}),

		/**
		 * Total items in paged results
		 */
		totalItems: computed(() => {
			return store.pagedValues()?.totalCount ?? 0;
		}),

		/**
		 * Current page number
		 */
		currentPage: computed(() => {
			return store.pagedValues()?.pageNumber ?? 0;
		}),

		/**
		 * Total pages
		 */
		totalPages: computed(() => {
			return store.pagedValues()?.totalPages ?? 0;
		})
	})),

	// Define methods
	withMethods((store, httpClient = inject(HttpClient)) => {
		return {
			// ========== Pagination ==========

			/**
			 * Load paginated Redis values
			 */
			loadPagedValues: rxMethod<{
				connectionId: string;
				pattern?: string;
				pageNumber?: number;
				pageSize?: number;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, pattern = '*', pageNumber = 1, pageSize = 50 }) => {
						const params = new HttpParams()
							.set('pattern', pattern)
							.set('pageNumber', pageNumber.toString())
							.set('pageSize', pageSize.toString());

						return httpClient
							.get<PagedRedisValuesResponse>(
								`/api/v1/storage/${connectionId}/keys`,
								{ params }
							)
							.pipe(
								tap((pagedValues) =>
									patchState(store, {
										pagedValues,
										isLoading: false
									})
								),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to load paged values',
										isLoading: false
									});
									return of(null);
								})
							);
					})
				)
			),

			// ========== Bulk Delete ==========

			/**
			 * Delete keys matching a pattern
			 */
			deleteKeysByPattern: rxMethod<{
				connectionId: string;
				pattern: string;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, pattern }) =>
						httpClient
							.delete<DeleteKeysByPatternResponse>(
								`/api/v1/storage/${connectionId}/keys/pattern/${encodeURIComponent(pattern)}`
							)
							.pipe(
								tap(() => patchState(store, { isLoading: false })),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to delete keys by pattern',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			/**
			 * Delete multiple specific keys
			 */
			deleteMultipleKeys: rxMethod<{
				connectionId: string;
				keys: string[];
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, keys }) =>
						httpClient
							.post<DeleteMultipleKeysResponse>(
								`/api/v1/storage/${connectionId}/keys/delete-multiple`,
								{ keys }
							)
							.pipe(
								tap(() => patchState(store, { isLoading: false })),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to delete multiple keys',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			// ========== Export/Import ==========

			/**
			 * Export Redis data to JSON or CSV
			 */
			exportData: rxMethod<{
				connectionId: string;
				pattern?: string;
				format?: 'json' | 'csv';
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, pattern = '*', format = 'json' }) => {
						const params = new HttpParams()
							.set('pattern', pattern)
							.set('format', format);

						return httpClient
							.get(`/api/v1/storage/${connectionId}/export`, {
								params,
								responseType: 'blob'
							})
							.pipe(
								tap(() => patchState(store, { isLoading: false })),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to export data',
										isLoading: false
									});
									return of(null);
								})
							);
					})
				)
			),

			/**
			 * Import Redis data from JSON or CSV
			 */
			importData: rxMethod<{
				connectionId: string;
				request: ImportRedisDataRequest;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, request }) =>
						httpClient
							.post<ImportRedisDataResponse>(
								`/api/v1/storage/${connectionId}/import`,
								request
							)
							.pipe(
								tap(() => patchState(store, { isLoading: false })),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to import data',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			// ========== Pub/Sub ==========

			/**
			 * Publish a message to a Redis channel
			 */
			publishMessage: rxMethod<{
				connectionId: string;
				request: PublishMessageRequest;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, request }) =>
						httpClient
							.post<void>(`/api/v1/pubsub/${connectionId}/publish`, request)
							.pipe(
								tap(() => patchState(store, { isLoading: false })),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to publish message',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			/**
			 * Subscribe to a Redis channel
			 */
			subscribeToChannel: rxMethod<{
				connectionId: string;
				channel: string;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, channel }) =>
						httpClient
							.post<SubscribeToChannelResponse>(
								`/api/v1/pubsub/${connectionId}/subscribe/${encodeURIComponent(channel)}`,
								{}
							)
							.pipe(
								tap((subscription) => {
									const subscriptions = [...store.activeSubscriptions(), subscription];
									patchState(store, {
										activeSubscriptions: subscriptions,
										isLoading: false
									});
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to subscribe to channel',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			/**
			 * Unsubscribe from a Redis channel
			 */
			unsubscribeFromChannel: rxMethod<{
				connectionId: string;
				subscriptionId: string;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, subscriptionId }) =>
						httpClient
							.delete<void>(
								`/api/v1/pubsub/${connectionId}/subscribe/${subscriptionId}`
							)
							.pipe(
								tap(() => {
									const subscriptions = store
										.activeSubscriptions()
										.filter((s) => s.subscriptionId !== subscriptionId);
									patchState(store, {
										activeSubscriptions: subscriptions,
										isLoading: false
									});
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to unsubscribe from channel',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			// ========== Transactions ==========

			/**
			 * Execute a Redis transaction
			 */
			executeTransaction: rxMethod<{
				connectionId: string;
				request: ExecuteTransactionRequest;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, request }) =>
						httpClient
							.post<ExecuteTransactionResponse>(
								`/api/v1/transaction/${connectionId}/execute`,
								request
							)
							.pipe(
								tap((result) =>
									patchState(store, {
										lastTransactionResult: result,
										isLoading: false
									})
								),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to execute transaction',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			// ========== Lua Scripts ==========

			/**
			 * Execute a Lua script
			 */
			executeLuaScript: rxMethod<{
				connectionId: string;
				request: ExecuteLuaScriptRequest;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, request }) =>
						httpClient
							.post<ExecuteLuaScriptResponse>(
								`/api/v1/script/${connectionId}/execute`,
								request
							)
							.pipe(
								tap((result) =>
									patchState(store, {
										lastScriptResult: result,
										isLoading: false
									})
								),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to execute Lua script',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			// ========== Health Monitoring ==========

			/**
			 * Load connection health status
			 */
			loadConnectionHealth: rxMethod<{ connectionId: string }>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId }) =>
						httpClient
							.get<GetConnectionHealthResponse>(
								`/api/v1/connections/${connectionId}/health`
							)
							.pipe(
								tap((health) =>
									patchState(store, {
										connectionHealth: health,
										isLoading: false
									})
								),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to load connection health',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			/**
			 * Clear error state
			 */
			clearError(): void {
				patchState(store, { error: null });
			},

			/**
			 * Clear transaction result
			 */
			clearTransactionResult(): void {
				patchState(store, { lastTransactionResult: null });
			},

			/**
			 * Clear script result
			 */
			clearScriptResult(): void {
				patchState(store, { lastScriptResult: null });
			},

			/**
			 * Reset store to initial state
			 */
			reset(): void {
				patchState(store, initialState);
			}
		};
	})
);
