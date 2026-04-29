import { ComponentRef, InjectionToken } from '@angular/core';
import { ComponentPortal } from '@angular/cdk/portal';
import { signalStore, withState, withMethods, patchState, withComputed } from '@ngrx/signals';
import { computed } from '@angular/core';

/**
 * Injection token for portal data
 * Used to pass data to portal components
 */
export const PORTAL_DATA = new InjectionToken<any>('PORTAL_DATA');

/**
 * Section actions portal state interface
 */
interface SectionActionsState {
	/**
	 * Current portal content
	 */
	portalContent: ComponentPortal<any> | null;

	/**
	 * Active component reference
	 */
	activeComponentRef: ComponentRef<any> | null;

	/**
	 * Whether portal has content
	 */
	hasContent: boolean;
}

/**
 * Initial state
 */
const initialState: SectionActionsState = {
	portalContent: null,
	activeComponentRef: null,
	hasContent: false
};

/**
 * Global section actions portal store using NgRx SignalStore
 * Replaces SectionActionsPortalService with signal-based state management
 * 
 * Provides centralized state for CDK Portal management in toolbar sections
 * 
 * @example
 * ```typescript
 * // Inject the store
 * private sectionActionsStore = inject(SectionActionsStore);
 * 
 * // Attach a component
 * const portal = new ComponentPortal(MyActionComponent);
 * this.sectionActionsStore.attach(portal);
 * 
 * // Check if portal has content
 * const hasContent = this.sectionActionsStore.hasContent();
 * 
 * // Clear portal
 * this.sectionActionsStore.clear();
 * ```
 */
export const SectionActionsStore = signalStore(
	{ providedIn: 'root' },

	// Define state
	withState(initialState),

	// Define computed signals
	withComputed((store) => ({
		/**
		 * Whether the portal currently has content
		 */
		isActive: computed(() => store.portalContent() !== null)
	})),

	// Define methods
	withMethods((store) => ({
		/**
		 * Attach a component to the portal
		 * @param component Component portal to render
		 */
		attach<T>(component: ComponentPortal<T>): void {
			patchState(store, {
				portalContent: component,
				hasContent: true
			});
		},

		/**
		 * Set reference to the currently active component
		 * @param componentRef Reference to the active component
		 */
		setActiveComponent(componentRef: ComponentRef<any>): void {
			patchState(store, {
				activeComponentRef: componentRef
			});
		},

		/**
		 * Clear the current portal content
		 * Destroys active component if exists
		 */
		clear(): void {
			// Destroy active component if exists
			const activeRef = store.activeComponentRef();
			if (activeRef) {
				activeRef.destroy();
			}

			patchState(store, {
				portalContent: null,
				activeComponentRef: null,
				hasContent: false
			});
		},

		/**
		 * Get the current portal content
		 * @returns Current portal content or null
		 */
		getPortalContent(): ComponentPortal<any> | null {
			return store.portalContent();
		},

		/**
		 * Get the active component reference
		 * @returns Active component reference or null
		 */
		getActiveComponentRef(): ComponentRef<any> | null {
			return store.activeComponentRef();
		}
	}))
);
