import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { SideNavLinkDefinition } from '../../models/side-nav.type';

/**
 * Default navigation routes
 */
const DEFAULT_ROUTES: SideNavLinkDefinition[] = [
  {
    href: '/',
    icon: 'dashboard',
    title: 'Dashboard',
    description: 'Manage your Redis connections'
  },
  {
    href: '/health',
    icon: 'monitor_heart',
    title: 'Health Monitoring',
    description: 'Monitor connection health status'
  },
  {
    href: '/audit',
    icon: 'history',
    title: 'Audit Logs',
    description: 'View system activity logs',
    badge: 'admin'
  },
  {
    href: '/info',
    icon: 'info',
    title: 'About',
    description: 'Application information'
  }
];

/**
 * Side navigation state interface
 */
interface SideNavState {
  /**
   * Current navigation links
   */
  links: SideNavLinkDefinition[];

  /**
   * Current active connection ID
   */
  currentConnectionId: string | null;

  /**
   * Current active connection name
   */
  currentConnectionName: string | null;
}

/**
 * Initial side nav state
 */
const initialState: SideNavState = {
  links: DEFAULT_ROUTES,
  currentConnectionId: null,
  currentConnectionName: null
};

export const SideNavStore = signalStore(
  { providedIn: 'root' },

  // Define state
  withState(initialState),

  // Define computed signals
  withComputed(store => ({
    /**
     * Whether a connection is currently active
     */
    hasActiveConnection: computed(() => store.currentConnectionId() !== null),

    /**
     * Get only connection-specific routes
     */
    connectionRoutes: computed(() => store.links().filter(link => link.href.includes('/storage/'))),

    /**
     * Get side nav links (alias for backward compatibility)
     */
    sideNavLinks: computed(() => store.links())
  })),

  // Define methods
  withMethods(store => ({
    /**
     * Add connection-specific routes to navigation
     * @param connectionId - The connection ID
     * @param connectionName - The display name for the connection
     */
    addConnectionRoutes(connectionId: string, connectionName: string): void {
      const connectionRoutes: SideNavLinkDefinition[] = [
        {
          href: `/storage/${connectionId}`,
          icon: 'storage',
          title: connectionName,
          description: 'Storage management',
          badge: 'active'
        }
      ];

      patchState(store, {
        links: [...DEFAULT_ROUTES, ...connectionRoutes],
        currentConnectionId: connectionId,
        currentConnectionName: connectionName
      });
    },

    /**
     * Clear connection-specific routes and restore default navigation
     */
    clearConnectionRoutes(): void {
      patchState(store, {
        links: [...DEFAULT_ROUTES],
        currentConnectionId: null,
        currentConnectionName: null
      });
    },

    /**
     * Add a custom route to the navigation
     * @param route - The route definition to add
     */
    addCustomRoute(route: SideNavLinkDefinition): void {
      patchState(store, state => ({
        links: [...state.links, route]
      }));
    },

    /**
     * Remove a route by href
     * @param href - The href of the route to remove
     */
    removeRoute(href: string): void {
      patchState(store, state => ({
        links: state.links.filter(link => link.href !== href)
      }));
    },

    /**
     * Reset to default navigation
     */
    reset(): void {
      patchState(store, initialState);
    }
  }))
);
