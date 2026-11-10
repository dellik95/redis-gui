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
	isLoading: false,
	error: null
};

/**
 * Global Redis operations store using NgRx SignalStore
 * Provides centralized state management for Redis core operations
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
				database?: number;
				onSuccess?: () => void;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, pattern, database, onSuccess }) => {
						let params = new HttpParams();
						if (database !== undefined && database !== null) {
							params = params.set('database', database.toString());
						}
						return httpClient
							.delete<DeleteKeysByPatternResponse>(
								`/api/v1/storage/${connectionId}/keys/pattern/${encodeURIComponent(pattern)}`,
								{ params }
							)
							.pipe(
								tap(() => {
									patchState(store, { isLoading: false });
									onSuccess?.();
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to delete keys by pattern',
										isLoading: false
									});
									return of(null);
								})
							);
					})
				)
			),

			/**
			 * Delete multiple specific keys
			 */
			deleteMultipleKeys: rxMethod<{
				connectionId: string;
				keys: string[];
				database?: number;
				onSuccess?: () => void;
			}>(
				pipe(
					tap(() => patchState(store, { isLoading: true, error: null })),
					switchMap(({ connectionId, keys, database, onSuccess }) => {
						let params = new HttpParams();
						if (database !== undefined && database !== null) {
							params = params.set('database', database.toString());
						}
						return httpClient
							.post<DeleteMultipleKeysResponse>(
								`/api/v1/storage/${connectionId}/keys/delete-multiple`,
								{ keys, database },
								{ params }
							)
							.pipe(
								tap(() => {
									patchState(store, { isLoading: false });
									onSuccess?.();
								}),
								catchError((error) => {
									patchState(store, {
										error: error.message || 'Failed to delete multiple keys',
										isLoading: false
									});
									return of(null);
								})
							);
					})
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
			 * Reset store to initial state
			 */
			reset(): void {
				patchState(store, initialState);
			}
		};
	})
);
