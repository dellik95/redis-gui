import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { User } from '../models/auth';

/**
 * Authentication state interface
 */
interface AuthState {
  /**
   * Currently authenticated user (null if not authenticated)
   */
  user: User | null;

  /**
   * Whether user is authenticated
   */
  isAuthenticated: boolean;

  /**
   * Authentication loading state
   */
  isLoading: boolean;

  /**
   * Authentication error message
   */
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
 * Provides centralized state management for user authentication
 * 
 * @example
 * ```typescript
 * // Inject the store
 * private authStore = inject(AuthStore);
 * 
 * // Read state
 * const user = this.authStore.user();
 * const isAdmin = this.authStore.isAdmin();
 * 
 * // Update state
 * this.authStore.setUser(user);
 * this.authStore.logout();
 * ```
 */
export const AuthStore = signalStore(
  { providedIn: 'root' },
  
  // Define state
  withState(initialState),
  
  // Define computed signals
  withComputed((store) => ({
    /**
     * Whether the current user has Admin role
     */
    isAdmin: computed(() => store.user()?.role === 'Admin'),
    
    /**
     * Whether the current user can perform write operations
     * True for Admin and User roles
     */
    canWrite: computed(() => {
      const role = store.user()?.role;
      return role === 'Admin' || role === 'User';
    }),
    
    /**
     * Whether the current user has read-only access
     */
    isReadOnly: computed(() => store.user()?.role === 'ReadOnly'),
    
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
      return user ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : null;
    })
  })),
  
  // Define methods
  withMethods((store) => ({
    /**
     * Sets the authenticated user and updates auth state
     */
    setUser(user: User): void {
      patchState(store, {
        user,
        isAuthenticated: true,
        error: null
      });
    },
    
    /**
     * Clears user data and resets auth state (logout)
     */
    clearUser(): void {
      patchState(store, initialState);
    },
    
    /**
     * Sets loading state
     */
    setLoading(isLoading: boolean): void {
      patchState(store, { isLoading });
    },
    
    /**
     * Sets error state
     */
    setError(error: string | null): void {
      patchState(store, { error, isLoading: false });
    },
    
    /**
     * Checks if user has specific role
     */
    hasRole(role: 'Admin' | 'User' | 'ReadOnly'): boolean {
      return store.user()?.role === role;
    },
    
    /**
     * Checks if user has any of the specified roles
     */
    hasAnyRole(...roles: ('Admin' | 'User' | 'ReadOnly')[]): boolean {
      const userRole = store.user()?.role;
      return userRole ? roles.includes(userRole) : false;
    },
    
    /**
     * Gets the current user's ID
     */
    getUserId(): string | null {
      return store.user()?.id ?? null;
    }
  }))
);
