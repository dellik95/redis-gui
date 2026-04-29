import { computed, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { catchError, of, pipe, switchMap, tap } from 'rxjs';
import type {
	AuditLogItem,
	GetAuditLogsResponse,
	GetAuditLogsRequest
} from '../../models/audit.types';

/**
 * Audit logs state interface
 */
interface AuditState {
	/**
	 * Audit log items
	 */
	items: AuditLogItem[];

	/**
	 * Total count of records
	 */
	totalCount: number;

	/**
	 * Current page skip value
	 */
	skip: number;

	/**
	 * Page size
	 */
	take: number;

	/**
	 * Loading state
	 */
	isLoading: boolean;

	/**
	 * Error message
	 */
	error: string | null;

	/**
	 * Current filters
	 */
	filters: GetAuditLogsRequest;
}

/**
 * Initial state
 */
const initialState: AuditState = {
	items: [],
	totalCount: 0,
	skip: 0,
	take: 50,
	isLoading: false,
	error: null,
	filters: {}
};

/**
 * Global audit log store using NgRx SignalStore
 * Provides centralized state management for audit logs
 *
 * Features:
 * - Paginated audit log retrieval
 * - Filtering by user, entity, and date range
 * - Admin-only access
 *
 * @example
 * ```typescript
 * // Inject the store
 * private auditStore = inject(AuditStore);
 *
 * // Load audit logs
 * this.auditStore.loadAuditLogs({});
 *
 * // Load with filters
 * this.auditStore.loadAuditLogs({
 *   entityType: 'Connection',
 *   fromDate: '2024-01-01',
 *   toDate: '2024-12-31'
 * });
 *
 * // Read state
 * const items = this.auditStore.items();
 * const totalCount = this.auditStore.totalCount();
 * ```
 */
export const AuditStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Whether there are any audit logs
		 */
		hasItems: computed(() => store.items().length > 0),

		/**
		 * Current page number (1-based)
		 */
		currentPage: computed(() => Math.floor(store.skip() / store.take()) + 1),

		/**
		 * Total number of pages
		 */
		totalPages: computed(() => Math.ceil(store.totalCount() / store.take())),

		/**
		 * Whether there's a next page
		 */
		hasNextPage: computed(() => store.skip() + store.take() < store.totalCount()),

		/**
		 * Whether there's a previous page
		 */
		hasPreviousPage: computed(() => store.skip() > 0)
	})),

	// Define methods
	withMethods((store, httpClient = inject(HttpClient)) => {
		const BASE_URL = '/api/v1/audit';

		return {
			/**
			 * Load audit logs with optional filters
			 */
			loadAuditLogs: rxMethod<GetAuditLogsRequest>(
				pipe(
					tap((request) =>
						patchState(store, {
							isLoading: true,
							error: null,
							filters: request
						})
					),
					switchMap((request) => {
						let params = new HttpParams();

						if (request.userId) params = params.set('userId', request.userId);
						if (request.entityType) params = params.set('entityType', request.entityType);
						if (request.entityId) params = params.set('entityId', request.entityId);
						if (request.fromDate) params = params.set('fromDate', request.fromDate);
						if (request.toDate) params = params.set('toDate', request.toDate);
						params = params.set('skip', (request.skip ?? store.skip()).toString());
						params = params.set('take', (request.take ?? store.take()).toString());

						return httpClient.get<GetAuditLogsResponse>(BASE_URL, { params }).pipe(
							tap((response) =>
								patchState(store, {
									items: response.items,
									totalCount: response.totalCount,
									skip: response.skip,
									take: response.take,
									isLoading: false
								})
							),
							catchError((error) => {
								patchState(store, {
									error: error.message || 'Failed to load audit logs',
									isLoading: false
								});
								return of(null);
							})
						);
					})
				)
			),

			/**
			 * Go to next page
			 */
			nextPage(): void {
				if (store.skip() + store.take() < store.totalCount()) {
					const newSkip = store.skip() + store.take();
					patchState(store, { skip: newSkip });
				}
			},

			/**
			 * Go to previous page
			 */
			previousPage(): void {
				if (store.skip() > 0) {
					const newSkip = Math.max(0, store.skip() - store.take());
					patchState(store, { skip: newSkip });
				}
			},

			/**
			 * Go to specific page
			 * @param page 1-based page number
			 */
			goToPage(page: number): void {
				const newSkip = (page - 1) * store.take();
				if (newSkip >= 0 && newSkip < store.totalCount()) {
					patchState(store, { skip: newSkip });
				}
			},

			/**
			 * Change page size
			 * @param pageSize New page size
			 */
			setPageSize(pageSize: number): void {
				patchState(store, { take: pageSize, skip: 0 });
			},

			/**
			 * Clear error state
			 */
			clearError(): void {
				patchState(store, { error: null });
			},

			/**
			 * Reset filters and state
			 */
			resetFilters(): void {
				patchState(store, {
					filters: {},
					skip: 0
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

