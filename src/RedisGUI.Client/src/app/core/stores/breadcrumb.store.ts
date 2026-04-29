import { computed, inject } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState, withHooks } from '@ngrx/signals';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Breadcrumb } from '../../models/breadcrumb.type';

/**
 * Breadcrumb state interface
 */
interface BreadcrumbState {
	/**
	 * Current breadcrumb trail
	 */
	breadcrumbs: Breadcrumb[];
}

/**
 * Initial breadcrumb state
 */
const initialState: BreadcrumbState = {
	breadcrumbs: []
};

/**
 * Global breadcrumb store using NgRx SignalStore
 * Provides centralized breadcrumb navigation state
 * 
 * Features:
 * - Automatic breadcrumb generation from routes
 * - Manual breadcrumb management
 * - Route-based updates
 * 
 * @example
 * ```typescript
 * // Inject the store
 * private breadcrumbStore = inject(BreadcrumbStore);
 * 
 * // Get breadcrumbs
 * const crumbs = this.breadcrumbStore.breadcrumbs();
 * 
 * // Manual update
 * this.breadcrumbStore.setBreadcrumbs([{ label: 'Home', url: '/' }]);
 * ```
 */
export const BreadcrumbStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Whether there are any breadcrumbs
		 */
		hasBreadcrumbs: computed(() => store.breadcrumbs().length > 0),

		/**
		 * Get the current (last) breadcrumb
		 */
		currentBreadcrumb: computed(() => {
			const crumbs = store.breadcrumbs();
			return crumbs.length > 0 ? crumbs[crumbs.length - 1] : null;
		}),

		/**
		 * Get breadcrumb count
		 */
		breadcrumbCount: computed(() => store.breadcrumbs().length)
	})),

	// Define methods
	withMethods((store) => {
		const router = inject(Router);
		const activatedRoute = inject(ActivatedRoute);

		/**
		 * Creates breadcrumb trail from route hierarchy
		 */
		const createBreadcrumbs = (
			route: ActivatedRoute,
			url: string = '',
			breadcrumbs: Breadcrumb[] = []
		): Breadcrumb[] => {
			const children: ActivatedRoute[] = route.children;

			if (children.length === 0) {
				return breadcrumbs;
			}

			for (const child of children) {
				const routeURL: string = child.snapshot.url.map(segment => segment.path).join('/');
				if (routeURL !== '') {
					url += `/${routeURL}`;
				}

				const label = child.snapshot.data['breadcrumb'];
				if (label) {
					breadcrumbs.push({ label, url });
				}
				return createBreadcrumbs(child, url, breadcrumbs);
			}

			return breadcrumbs;
		};

		return {
			/**
			 * Initialize route subscription for automatic breadcrumb updates
			 */
			initRouteSubscription(): void {
				router.events.pipe(
					filter(event => event instanceof NavigationEnd)
				).subscribe(() => {
					const breadcrumbs = createBreadcrumbs(activatedRoute.root);
					patchState(store, { breadcrumbs });
				});
			},

			/**
			 * Manually set breadcrumbs
			 * @param breadcrumbs - Array of breadcrumb objects
			 */
			setBreadcrumbs(breadcrumbs: Breadcrumb[]): void {
				patchState(store, { breadcrumbs });
			},

			/**
			 * Add a breadcrumb to the trail
			 * @param breadcrumb - Breadcrumb to add
			 */
			addBreadcrumb(breadcrumb: Breadcrumb): void {
				patchState(store, (state) => ({
					breadcrumbs: [...state.breadcrumbs, breadcrumb]
				}));
			},

			/**
			 * Clear all breadcrumbs
			 */
			clear(): void {
				patchState(store, { breadcrumbs: [] });
			},

			/**
			 * Reset to initial state
			 */
			reset(): void {
				patchState(store, initialState);
			}
		};
	}),

	// Initialize route subscription on store creation
	withHooks({
		onInit(store) {
			store.initRouteSubscription();
		}
	})
);







