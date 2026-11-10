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
		database?: number;
	} | null;

	/**
	 * Active database index (DB 0-15)
	 */
	activeDatabase: number;

	/**
	 * Keyspace stats (DB -> count)
	 */
	keyspace: Record<number, number>;

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
	activeDatabase: 0,
	keyspace: {},
	lastFetched: null
};

/**
 * Global Redis keys store using NgRx SignalStore
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
			 * Set active database
			 */
			setActiveDatabase(db: number) {
				patchState(store, { activeDatabase: db, lastFetched: null });
			},

			/**
			 * Load keyspace info
			 */
			loadKeyspace: rxMethod<{ connectionId: string }>(
				pipe(
					switchMap(({ connectionId }) =>
						httpClient.get<{ databases: Record<number, number> }>(`${BASE_URL}/${connectionId}/keyspace`).pipe(
							tap(res => {
								if (res?.databases) {
									patchState(store, { keyspace: res.databases });
								}
							}),
							catchError(() => of(null))
						)
					)
				)
			),

			/**
			 * Load Redis keys (fetches all keys - components handle grouping/display)
			 */
			loadKeys: rxMethod<{
				connectionId: string;
				pattern?: string | undefined;
				database?: number | undefined;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, pattern = '*', database }) => {
						const targetDb = database ?? store.activeDatabase();

						// Fetch all keys without pagination
						let params = new HttpParams().set('pattern', pattern);
						if (targetDb !== undefined && targetDb !== null) {
							params = params.set('database', targetDb.toString());
						}

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
										activeDatabase: targetDb,
										lastFetchParams: {
											connectionId,
											pattern,
											database: targetDb
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
			loadValue: rxMethod<{ connectionId: string; key: string; database?: number }>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, key, database }) => {
						const targetDb = database ?? store.activeDatabase();
						let params = new HttpParams();
						if (targetDb !== undefined && targetDb !== null) {
							params = params.set('database', targetDb.toString());
						}

						return httpClient
							.get<{ key: string; value: any; type: string; timeToLive?: string; ttl?: number }>(
								`${BASE_URL}/${connectionId}/${encodeURIComponent(key)}`,
								{ params }
							)
							.pipe(
								tap((response) => {
									let ttlVal = response.ttl ?? -1;
									if (response.timeToLive && ttlVal === -1) {
										const parts = response.timeToLive.split(':');
										if (parts.length === 3) {
											ttlVal = Math.round(parseFloat(parts[0] ?? '0') * 3600 + parseFloat(parts[1] ?? '0') * 60 + parseFloat(parts[2] ?? '0'));
										}
									}

									// Standardize type matching
									let mappedType = RedisDataType.String;
									if (response.type) {
										const t = response.type.toLowerCase();
										if (t === 'list') mappedType = RedisDataType.List;
										else if (t === 'set') mappedType = RedisDataType.Set;
										else if (t === 'hash') mappedType = RedisDataType.Hash;
										else if (t === 'sortedset' || t === 'zset') mappedType = RedisDataType.SortedSet;
										else if (t === 'stream') mappedType = RedisDataType.Stream;
									}

									const value: RedisKeyValue = {
										key: response.key,
										value: response.value,
										type: mappedType,
										ttl: ttlVal
									};
									patchState(store, {
										selectedKey: value,
										isLoading: false
									});
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to load value',
										isLoading: false
									});
									return of(null);
								})
							);
					})
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
				database?: number;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, key, type, value, ttl, database }) => {
						const targetDb = database ?? store.activeDatabase();

						const requestBody = {
							connectionId,
							key,
							type,
							value,
							ttl: ttl,
							database: targetDb
						};

						let params = new HttpParams();
						if (targetDb !== undefined && targetDb !== null) {
							params = params.set('database', targetDb.toString());
						}

						return httpClient
							.put<void>(
								`${BASE_URL}/${connectionId}/keys/${encodeURIComponent(key)}`,
								requestBody,
								{ params }
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
				type?: RedisDataType;
				value: any;
				ttl?: number;
				database?: number;
				onSuccess?: () => void;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, key, type = RedisDataType.String, value, ttl, database, onSuccess }) => {
						const targetDb = database ?? store.activeDatabase();

						const valObj = typeof value === 'object' && value !== null
							? value
							: { value: String(value ?? '') };

						let params = new HttpParams();
						if (targetDb !== undefined && targetDb !== null) {
							params = params.set('database', targetDb.toString());
						}

						return httpClient
							.post<void>(`${BASE_URL}/${connectionId}/keys`, {
								connectionId,
								key,
								type,
								value: valObj,
								ttl: ttl,
								database: targetDb
							}, { params })
							.pipe(
								tap(() => {
									patchState(store, {
										isLoading: false,
										lastFetched: null // Invalidate cache
									});
									onSuccess?.();
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to add value',
										isLoading: false
									});
									return of(null);
								})
							);
					})
				)
			),

			/**
			 * Delete a Redis key
			 */
			deleteKey: rxMethod<{ connectionId: string; key: string; database?: number; onSuccess?: () => void }>(
				pipe(
					tap(({ key }) => {
						const currentKeys = store.keys();
						patchState(store, {
							keys: currentKeys.filter(k => k.key !== key),
							selectedKey: store.selectedKey()?.key === key ? null : store.selectedKey(),
							isLoading: true,
							error: null,
							lastFetched: null,
							lastFetchParams: null
						});
					}),
					switchMap(({ connectionId, key, database, onSuccess }) => {
						const targetDb = database ?? store.activeDatabase();
						let params = new HttpParams();
						if (targetDb !== undefined && targetDb !== null) {
							params = params.set('database', targetDb.toString());
						}

						return httpClient
							.delete<boolean>(
								`${BASE_URL}/${connectionId}/keys/${encodeURIComponent(key)}`,
								{ params }
							)
							.pipe(
								tap(() => {
									patchState(store, {
										isLoading: false,
										lastFetched: null,
										lastFetchParams: null
									});
									onSuccess?.();
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to delete key',
										isLoading: false
									});
									return of(null);
								})
							);
					})
				)
			),

			/**
			 * Seed demo sample data across DB 0, DB 1, DB 2
			 */
			seedDemoData: rxMethod<{ connectionId: string; onSuccess?: () => void }>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, onSuccess }) =>
						httpClient.post<{ seededCount: number; message: string }>(`${BASE_URL}/${connectionId}/seed`, {}).pipe(
							tap(() => {
								patchState(store, { isLoading: false, lastFetched: null });
								onSuccess?.();
							}),
							catchError((error) => {
								patchState(store, {
									error: error.error?.detail || error.error?.message || error.message || 'Failed to seed sample data',
									isLoading: false
								});
								return of(null);
							})
						)
					)
				)
			),

			/**
			 * Rename a key
			 */
			renameKey: rxMethod<{
				connectionId: string;
				oldKey: string;
				newKey: string;
				overwrite?: boolean;
				database?: number;
				onSuccess?: () => void;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, oldKey, newKey, overwrite = false, database, onSuccess }) =>
						httpClient.post(`${BASE_URL}/${connectionId}/keys/rename`, {
							oldKey,
							newKey,
							overwrite,
							database
						}).pipe(
							tap(() => {
								patchState(store, { isLoading: false, lastFetched: null });
								onSuccess?.();
							}),
							catchError((error) => {
								patchState(store, {
									error: error.error?.detail || error.error?.message || error.message || 'Failed to rename key',
									isLoading: false
								});
								return of(null);
							})
						)
					)
				)
			),

			/**
			 * Duplicate a key
			 */
			duplicateKey: rxMethod<{
				connectionId: string;
				sourceKey: string;
				targetKey: string;
				database?: number;
				onSuccess?: () => void;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, sourceKey, targetKey, database, onSuccess }) =>
						httpClient.post(`${BASE_URL}/${connectionId}/keys/duplicate`, {
							sourceKey,
							targetKey,
							database
						}).pipe(
							tap(() => {
								patchState(store, { isLoading: false, lastFetched: null });
								onSuccess?.();
							}),
							catchError((error) => {
								patchState(store, {
									error: error.error?.detail || error.error?.message || error.message || 'Failed to duplicate key',
									isLoading: false
								});
								return of(null);
							})
						)
					)
				)
			),

			/**
			 * Bulk update TTL or persist
			 */
			bulkUpdateTtl: rxMethod<{
				connectionId: string;
				keys: string[];
				ttlSeconds: number | null;
				database?: number;
				onSuccess?: (count: number) => void;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, keys, ttlSeconds, database, onSuccess }) =>
						httpClient.post<{ updatedCount: number }>(`${BASE_URL}/${connectionId}/keys/bulk-ttl`, {
							keys,
							ttlSeconds,
							database
						}).pipe(
							tap((res) => {
								patchState(store, { isLoading: false, lastFetched: null });
								onSuccess?.(res?.updatedCount ?? keys.length);
							}),
							catchError((error) => {
								patchState(store, {
									error: error.error?.detail || error.error?.message || error.message || 'Failed to update TTL',
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
