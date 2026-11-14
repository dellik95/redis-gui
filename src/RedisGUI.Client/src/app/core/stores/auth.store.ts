import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { User, UserRole } from '../models/auth';

/**
 * Authentication state interface
 */
interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

/**
 * Initial authentication state
 */
const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: false,
  error: null
};

/**
 * Global authentication store using NgRx SignalStore
 */
export const AuthStore = signalStore(
  { providedIn: 'root' },

  // Define state
  withState(initialState),

  // Define computed signals
  withComputed(store => ({
    /**
     * Whether the current user has Admin role
     */
    isAdmin: computed(() => {
      const u = store.user();
      return u?.role === UserRole.Admin;
    }),

    /**
     * Whether the current user can perform write operations
     */
    canWrite: computed(() => {
      const u = store.user();
      return u?.role === UserRole.Admin || u?.role === UserRole.User;
    }),

    /**
     * Whether the current user has read-only access
     */
    isReadOnly: computed(() => {
      const u = store.user();
      return u?.role === UserRole.ReadOnly;
    }),

    /**
     * User's full name (firstName + lastName)
     */
    userFullName: computed(() => {
      const user = store.user();
      return user ? `${user.firstName} ${user.lastName}` : null;
    }),

    /**
     * User's initials for avatar display
     */
    userInitials: computed(() => {
      const user = store.user();
      if (user && user.firstName && user.lastName) {
        return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
      }
      return '';
    })
  })),

  // Define methods
  withMethods(store => ({
    setUser(user: User): void {
      patchState(store, {
        user,
        isAuthenticated: true,
        error: null
      });
    },

    clearUser(): void {
      patchState(store, initialState);
    },

    setLoading(isLoading: boolean): void {
      patchState(store, { isLoading });
    },

    setError(error: string | null): void {
      patchState(store, { error, isLoading: false });
    },

    hasRole(role: UserRole): boolean {
      const currentRole = store.user()?.role;
      return currentRole === role;
    },

    hasAnyRole(...roles: UserRole[]): boolean {
      const currentRole = store.user()?.role;
      return currentRole !== undefined && roles.includes(currentRole);
    },

    getUserId(): string | null {
      return store.user()?.id ?? null;
    }
  }))
);
