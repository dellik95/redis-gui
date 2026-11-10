import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { Observable, of } from 'rxjs';
import { concatMap, finalize } from 'rxjs/operators';

/**
 * Loading state interface
 */
interface LoadingState {
	/**
	 * Whether loading is active
	 */
	isLoading: boolean;

	/**
	 * Number of active loading requests (for concurrent operations)
	 */
	activeRequests: number;
}

/**
 * Initial loading state
 */
const initialState: LoadingState = {
	isLoading: false,
	activeRequests: 0
};

/**
 * Global loading store using NgRx SignalStore
 * Provides centralized loading state management
 * Compatible with Angular 21 zoneless mode
 *
 * Features:
 * - Track loading state
 * - Support concurrent loading operations
 * - Wrap observables with loading indicators
 * - Automatic request counting
 *
 * @example
 * ```typescript
 * // Inject the store
 * private loadingStore = inject(LoadingStore);
 *
 * // Check loading state (reactive signal)
 * const isLoading = this.loadingStore.isLoading();
 *
 * // Manual loading control
 * this.loadingStore.loadingOn();
 * this.loadingStore.loadingOff();
 *
 * // Wrap observable (automatically manages loading state)
 * this.loadingStore.showLoaderUntilCompleted(myObservable$);
 * ```
 */
export const LoadingStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Whether any loading operation is active
		 */
		hasActiveLoading: computed(() => store.activeRequests() > 0),

		/**
		 * Loading progress indicator (0-100%)
		 * Useful for progress bars
		 */
		loadingProgress: computed(() => {
			const requests = store.activeRequests();
			return requests > 0 ? Math.min(100, requests * 10) : 0;
		})
	})),

	// Define methods
	withMethods((store) => ({
		/**
		 * Activate loading state
		 * Increments active request counter
		 */
		loadingOn(): void {
			patchState(store, (state) => ({
				isLoading: true,
				activeRequests: state.activeRequests + 1
			}));
		},

		/**
		 * Deactivate loading state
		 * Decrements active request counter
		 */
		loadingOff(): void {
			patchState(store, (state) => {
				const newActiveRequests = Math.max(0, state.activeRequests - 1);
				return {
					isLoading: newActiveRequests > 0,
					activeRequests: newActiveRequests
				};
			});
		},

		/**
		 * Force reset loading state
		 * Clears all active requests
		 */
		reset(): void {
			patchState(store, initialState);
		},

		/**
		 * Wraps an observable with loading indicators
		 * Shows loading state while the observable is executing
		 * Automatically manages request counting and error handling
		 *
		 * @param obs$ - The observable to wrap
		 * @returns The same observable with loading side effects
		 *
		 * @example
		 * ```typescript
		 * this.loadingStore.showLoaderUntilCompleted(
		 *   this.http.get('/api/data')
		 * ).subscribe(data => {
		 *   // Loading indicator automatically shown/hidden
		 * });
		 * ```
		 */
		showLoaderUntilCompleted<T>(obs$: Observable<T>): Observable<T> {
			return of(null).pipe(
				concatMap(() => {
					// Increment loading counter
					patchState(store, (state) => ({
						isLoading: true,
						activeRequests: state.activeRequests + 1
					}));
					return obs$;
				}),
				finalize(() => {
					// Decrement loading counter
					patchState(store, (state) => {
						const newActiveRequests = Math.max(0, state.activeRequests - 1);
						return {
							isLoading: newActiveRequests > 0,
							activeRequests: newActiveRequests
						};
					});
				})
			);
		}
	}))
);







