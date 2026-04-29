import { computed, inject } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, tap, catchError, of, switchMap, map } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
	RedisDataType,
	RedisValueData,
	UpdateRedisValueRequest,
	RedisKeyInfo
} from '../../models/redis-keys.types';
import { PagedRedisValuesResponse, RedisKeyValuePair } from '../../models/redis-advanced.types';

/**
 * Redis key with value data
 */
export interface RedisKeyValue {
	key: string;
	value: any;
	type: RedisDataType;
	ttl?: number;
}

/**
 * Redis keys state interface
 */
interface RedisKeysState {
	/**
	 * Raw Redis keys (not grouped - components handle grouping)
	 */
	keys: RedisKeyInfo[];

	/**
	 * Currently selected key
	 */
	selectedKey: RedisKeyValue | null;

	/**
	 * Loading state
	 */
	isLoading: boolean;

	/**
	 * Error message
	 */
	error: string | null;

	/**
	 * Last fetch parameters for cache validation
	 */
	lastFetchParams: {
		connectionId: string;
		pattern: string;
	} | null;

	/**
	 * Last fetch timestamp
	 */
	lastFetched: number | null;
}

/**
 * Initial state
 */
const initialState: RedisKeysState = {
	keys: [],
	selectedKey: null,
	isLoading: false,
	error: null,
	lastFetchParams: null,
	lastFetched: null
};

/**
 * Global Redis keys store using NgRx SignalStore
 * Replaces RedisKeysService caching with centralized state management
 *
 * Features:
 * - Raw key fetching (components handle grouping/display)
 * - Automatic caching with 5-second TTL
 * - Type-safe Redis value operations
 * - Support for all Redis data types (string, hash, list, set, zset)
 *
 * @example
 * ```typescript
 * // Inject the store
 * private redisKeysStore = inject(RedisKeysStore);
 *
 * // Load keys
 * this.redisKeysStore.loadKeys({
 *   connectionId: 'conn-123',
 *   pattern: 'user:*'
 * });
 *
 * // Read state
 * const keys = this.redisKeysStore.keys();
 * const selectedKey = this.redisKeysStore.selectedKey();
 *
 * // Update value
 * this.redisKeysStore.updateValue({
 *   connectionId: 'conn-123',
 *   key: 'mykey',
 *   type: 'string',
 *   value: 'myvalue',
 *   ttl: 3600
 * });
 * ```
 */
export const RedisKeysStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Whether cache needs refresh (older than 5 seconds)
		 */
		needsRefresh: computed(() => {
			const lastFetched = store.lastFetched();
			if (!lastFetched) return true;
			return Date.now() - lastFetched > 5000; // 5 seconds
		}),

		/**
		 * Total number of keys
		 */
		totalKeys: computed(() => {
			return store.keys().length;
		}),

		/**
		 * Whether there are any keys
		 */
		hasKeys: computed(() => {
			return store.keys().length > 0;
		})
	})),

	// Define methods
	withMethods((store, httpClient = inject(HttpClient)) => {
		const BASE_URL = '/api/v1/storage';
		const CACHE_DURATION = 5000;

		/**
		 * Convert RedisKeyValuePair to RedisKeyInfo
		 */
		const convertToKeyInfo = (pair: RedisKeyValuePair): RedisKeyInfo => {
			return {
				key: pair.key,
				type: pair.type,
				size: pair.value?.length || 0,
				ttl: pair.timeToLive ? Math.floor(pair.timeToLive / 1000) : null,
				memoryUsage: null
			};
		};

		return {
			/**
			 * Load Redis keys (fetches all keys - components handle grouping/display)
			 */
			loadKeys: rxMethod<{
				connectionId: string;
				pattern?: string;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, pattern = '*' }) => {
						// Check if we have valid cached data
						const lastParams = store.lastFetchParams();
						const needsRefresh = store.needsRefresh();

						if (
							!needsRefresh &&
							lastParams &&
							lastParams.connectionId === connectionId &&
							lastParams.pattern === pattern
						) {
							// Return cached data
							patchState(store, { isLoading: false });
							return of(null);
						}

						// Fetch all keys without pagination
						const params = new HttpParams().set('pattern', pattern);

						return httpClient
							.get<PagedRedisValuesResponse>(`${BASE_URL}/${connectionId}`, { params })
							.pipe(
								map((response) => {
									// Convert RedisKeyValuePair[] to RedisKeyInfo[]
									return response.values.map(pair => convertToKeyInfo(pair));
								}),
								tap((keys) =>
									patchState(store, {
										keys,
										isLoading: false,
										lastFetchParams: {
											connectionId,
											pattern
										},
										lastFetched: Date.now()
									})
								),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to load keys',
										isLoading: false
									});
									return of(null);
								})
							);
					})
				)
			),

			/**
			 * Load a single Redis value by key
			 */
			loadValue: rxMethod<{ connectionId: string; key: string }>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, key }) =>
						httpClient
							.get<RedisKeyValue>(
								`${BASE_URL}/${connectionId}/${encodeURIComponent(key)}`
							)
							.pipe(
								tap((value) =>
									patchState(store, {
										selectedKey: value,
										isLoading: false
									})
								),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to load value',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			/**
			 * Update a Redis value
			 */
			updateValue: rxMethod<{
				connectionId: string;
				key: string;
				type: RedisDataType;
				value: RedisValueData;
				ttl?: number;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, key, type, value, ttl }) => {
						const typeToNumber: Record<RedisDataType, number> = {
							[RedisDataType.String]: 0,
							[RedisDataType.List]: 1,
							[RedisDataType.Set]: 2,
							[RedisDataType.Hash]: 3,
							[RedisDataType.SortedSet]: 4,
							[RedisDataType.Stream]: 5
						};

						const requestBody = {
							connectionId,
							key,
							type: typeToNumber[type], // Send as numeric enum value
							value,
							ttl: ttl
						};

						return httpClient
							.put<void>(
								`${BASE_URL}/${connectionId}/keys/${encodeURIComponent(key)}`,
								requestBody
							)
							.pipe(
								tap(() => {
									patchState(store, {
										isLoading: false,
										lastFetched: null // Invalidate cache
									});
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to update value',
										isLoading: false
									});
									return of(null);
								})
							);
					})
				)
			),

			/**
			 * Add a new Redis key-value pair
			 */
			addValue: rxMethod<{
				connectionId: string;
				key: string;
				value: string;
				ttl?: number;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, key, value, ttl }) =>
						httpClient
							.post<void>(`${BASE_URL}/${connectionId}/keys`, {
								key,
								value,
								ttl
							})
							.pipe(
								tap(() => {
									patchState(store, {
										isLoading: false,
										lastFetched: null // Invalidate cache
									});
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to add value',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			/**
			 * Delete a Redis key
			 */
			deleteKey: rxMethod<{ connectionId: string; key: string }>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, key }) =>
						httpClient
							.delete<boolean>(
								`${BASE_URL}/${connectionId}/keys/${encodeURIComponent(key)}`
							)
							.pipe(
								tap(() => {
									patchState(store, {
										isLoading: false,
										lastFetched: null, // Invalidate cache
										selectedKey:
											store.selectedKey()?.key === key
												? null
												: store.selectedKey()
									});
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to delete key',
										isLoading: false
									});
									return of(null);
								})
							)
					)
				)
			),

			/**
			 * Select a key for editing
			 */
			selectKey(key: RedisKeyValue | null): void {
				patchState(store, { selectedKey: key });
			},

			/**
			 * Clear error state
			 */
			clearError(): void {
				patchState(store, { error: null });
			},

			/**
			 * Clear cache and force refresh on next load
			 */
			clearCache(): void {
				patchState(store, {
					lastFetched: null,
					lastFetchParams: null
				});
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
