import { Injectable, signal, computed } from '@angular/core';
import { User } from '../../models/auth';

/**
 * Service for managing JWT tokens and user data in localStorage with signal-based reactivity
 *
 * Features:
 * - Token persistence and retrieval
 * - User data storage
 * - Token expiration validation
 * - Secure token management
 * - Reactive signals for token state
 *
 * @example
 * ```typescript
 * private tokenStorage = inject(TokenStorageService);
 *
 * // Save token
 * this.tokenStorage.saveToken(token);
 *
 * // Check if token is valid (reactive)
 * if (this.tokenStorage.hasValidToken()) {
 *   // Token is valid
 * }
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {
  // 1. Constants
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'current_user';

  // 2. Signal-based state
  /** Current JWT token (reactive) */
  private readonly _token = signal<string | null>(
    localStorage.getItem(this.TOKEN_KEY)
  );

  /** Current user data (reactive) */
  private readonly _user = signal<User | null>(
    this.getUserFromStorage()
  );

  // 3. Computed signals for token validation
  /** Whether token is expired (reactive) */
  readonly isTokenExpired = computed(() => {
    const token = this._token();
    if (!token) return true;

    try {
      // Decode JWT payload (second part of token)
      const parts = token.split('.');
      if (parts.length !== 3) return true;

      const payload = JSON.parse(atob(parts[1]!));

      // Check if expiration claim exists and if token is expired
      if (!payload.exp) return true;

      // JWT exp is in seconds, Date.now() is in milliseconds
      return payload.exp * 1000 < Date.now();
    } catch {
      // If token is malformed or cannot be decoded, consider it expired
      return true;
    }
  });

  /** Whether a valid token exists (reactive) */
  readonly hasValidToken = computed(() =>
    !!this._token() && !this.isTokenExpired()
  );

  /** Current token value (readonly signal) */
  readonly token = this._token.asReadonly();

  /** Current user value (readonly signal) */
  readonly user = this._user.asReadonly();

  // 4. Private helper methods
  /**
   * Retrieves user data from localStorage during initialization
   */
  private getUserFromStorage(): User | null {
    try {
      const user = localStorage.getItem(this.USER_KEY);
      return user ? JSON.parse(user) : null;
    } catch {
      return null;
    }
  }

  // 5. Public API methods
  /**
   * Saves JWT token to localStorage and updates signal
   */
  saveToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this._token.set(token);
  }

  /**
   * Retrieves JWT token from signal (reactive)
   */
  getToken(): string | null {
    return this._token();
  }

  /**
   * Saves user data to localStorage and updates signal
   */
  saveUser(user: User): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this._user.set(user);
  }

  /**
   * Retrieves user data from signal (reactive)
   */
  getUser(): User | null {
    return this._user();
  }

  /**
   * Clears all authentication data from localStorage and signals
   */
  clear(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this._token.set(null);
    this._user.set(null);
  }
}
