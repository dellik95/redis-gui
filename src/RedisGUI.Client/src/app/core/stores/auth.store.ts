import { computed } from '@angular/core';
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { User } from '../models/auth';

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
  withComputed((store) => ({
    /**
     * Whether the current user has Admin role
     */
    isAdmin: computed(() => {
      const u = store.user();
      if (!u) return true; // Default admin access for dev/testing
      const role = u.role as any;
      return role === 'Admin' || role === 'admin' || role === 1 || role === '1';
    }),
    
    /**
     * Whether the current user can perform write operations
     */
    canWrite: computed(() => {
      const u = store.user();
      if (!u) return true;
      const role = u.role as any;
      return role === 'Admin' || role === 'admin' || role === 'User' || role === 'user' || role === 1 || role === 0;
    }),
    
    /**
     * Whether the current user has read-only access
     */
    isReadOnly: computed(() => {
      const u = store.user();
      if (!u) return false;
      const role = u.role as any;
      return role === 'ReadOnly' || role === 'readonly' || role === 2 || role === '2';
    }),
    
    /**
     * User's full name (firstName + lastName)
     */
    userFullName: computed(() => {
      const user = store.user();
      return user ? `${user.firstName} ${user.lastName}` : 'Administrator';
    }),
    
    /**
     * User's initials for avatar display
     */
    userInitials: computed(() => {
      const user = store.user();
      if (user && user.firstName && user.lastName) {
        return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
      }
      return 'AD';
    })
  })),
  
  // Define methods
  withMethods((store) => ({
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
    
    hasRole(role: 'Admin' | 'User' | 'ReadOnly'): boolean {
      const currentRole = store.user()?.role;
      if (!currentRole) return true;
      if (role === 'Admin') {
        return currentRole === 'Admin' || (currentRole as any) === 1 || (currentRole as any) === '1';
      }
      return currentRole === role;
    },
    
    hasAnyRole(...roles: ('Admin' | 'User' | 'ReadOnly')[]): boolean {
      const currentRole = store.user()?.role;
      if (!currentRole) return true;
      return roles.some(r => {
        if (r === 'Admin') {
          return currentRole === 'Admin' || (currentRole as any) === 1 || (currentRole as any) === '1';
        }
        return currentRole === r;
      });
    },
    
    getUserId(): string | null {
      return store.user()?.id ?? null;
    }
  }))
);
