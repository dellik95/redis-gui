import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { SideNavLinkDefinition } from '../../models/side-nav.type';

/**
 * Default navigation routes
 */
const DEFAULT_ROUTES: SideNavLinkDefinition[] = [
  {
    href: '/home',
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

interface PersistedConnectionRoute {
  connectionId: string;
  connectionName: string;
  serverMode?: string | undefined;
}

const STORAGE_PREFIX = 'redisgui:sidenav:v1:';

interface SideNavState {
  userScope: string | null;
  links: SideNavLinkDefinition[];
  currentConnectionId: string | null;
  currentConnectionName: string | null;
  currentConnectionServerMode: string | null;
}

const initialState: SideNavState = {
  userScope: null,
  links: DEFAULT_ROUTES,
  currentConnectionId: null,
  currentConnectionName: null,
  currentConnectionServerMode: null
};

export const SideNavStore = signalStore(
  { providedIn: 'root' },

  withState(initialState),

  withComputed(store => ({
    hasActiveConnection: computed(() => store.currentConnectionId() !== null),
    connectionRoutes: computed(() => store.links().filter(link => link.href.includes('/storage/'))),
    sideNavLinks: computed(() => store.links())
  })),

  withMethods(store => {
    const storageKey = (scope: string) => `${STORAGE_PREFIX}${scope}`;

    const persistActiveConnection = (
      scope: string | null,
      connectionId: string | null,
      connectionName: string | null,
      serverMode: string | null
    ) => {
      if (!scope) return;
      try {
        if (!connectionId || !connectionName) {
          sessionStorage.removeItem(storageKey(scope));
        } else {
          sessionStorage.setItem(
            storageKey(scope),
            JSON.stringify({ connectionId, connectionName, serverMode })
          );
        }
      } catch {
        /* Ignore storage errors */
      }
    };

    const buildConnectionRoutes = (
      connectionId: string,
      connectionName: string,
      serverMode?: string | null
    ): SideNavLinkDefinition[] => [
      {
        href: `/storage/${connectionId}`,
        icon: 'storage',
        title: connectionName,
        description: 'Storage management',
        badge: serverMode === 'Cluster' ? 'Cluster' : 'active'
      },
      {
        href: `/messaging/${connectionId}`,
        icon: 'forum',
        title: 'Messaging Inspector',
        description: 'Pub/sub and streams'
      }
    ];

    return {
      setUserScope(userScope: string | null): void {
        patchState(store, { userScope });
      },

      getPersistedConnection(userScope: string): PersistedConnectionRoute | null {
        try {
          const raw = sessionStorage.getItem(storageKey(userScope));
          if (!raw) return null;
          const parsed = JSON.parse(raw) as PersistedConnectionRoute;
          return parsed?.connectionId && parsed?.connectionName ? parsed : null;
        } catch {
          return null;
        }
      },

      addConnectionRoutes(
        connectionId: string,
        connectionName: string,
        serverMode?: string | null
      ): void {
        const resolvedMode = serverMode ?? null;
        patchState(store, {
          links: [...DEFAULT_ROUTES, ...buildConnectionRoutes(connectionId, connectionName, resolvedMode)],
          currentConnectionId: connectionId,
          currentConnectionName: connectionName,
          currentConnectionServerMode: resolvedMode
        });
        persistActiveConnection(store.userScope(), connectionId, connectionName, resolvedMode);
      },

      clearConnectionRoutes(): void {
        patchState(store, {
          links: [...DEFAULT_ROUTES],
          currentConnectionId: null,
          currentConnectionName: null,
          currentConnectionServerMode: null
        });
        persistActiveConnection(store.userScope(), null, null, null);
      },

      addCustomRoute(route: SideNavLinkDefinition): void {
        patchState(store, state => ({
          links: [...state.links, route]
        }));
      },

      removeRoute(href: string): void {
        patchState(store, state => ({
          links: state.links.filter(link => link.href !== href)
        }));
      },

      reset(): void {
        patchState(store, initialState);
      },

      resetSession(clearPersisted = false): void {
        const scope = store.userScope();
        if (clearPersisted && scope) {
          persistActiveConnection(scope, null, null, null);
        }
        patchState(store, initialState);
      }
    };
  })
);
