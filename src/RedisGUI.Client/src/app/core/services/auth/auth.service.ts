import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { TokenStorageService } from './token-storage.service';
import { AuthStore } from '../../stores/auth.store';
import { 
  User, 
  LoginRequest, 
  LoginResponse, 
  RegisterRequest, 
  RegisterResponse 
} from '../../models/auth';

/**
 * Service for handling user authentication
 * 
 * Features:
 * - User registration and login
 * - Token management and persistence
 * - Authentication state management
 * - Role-based access control
 * 
 * Integrates with AuthStore for centralized state management
 * 
 * @example
 * ```typescript
 * private authService = inject(AuthService);
 * 
 * // Login
 * this.authService.login({ email, password }).subscribe();
 * 
 * // Check authentication
 * if (this.authService.isAuthenticated()) {
 *   // User is authenticated
 * }
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // 1. Injected dependencies
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly authStore = inject(AuthStore);

  // 2. Constants
  private readonly API_BASE = '/api/v1/auth';

  // 3. Exposed store signals (for backward compatibility and convenience)
  /** Current authenticated user */
  readonly currentUser = this.authStore.user;
  
  /** Whether user is authenticated */
  readonly isAuthenticated = this.authStore.isAuthenticated;
  
  /** Whether user has admin role */
  readonly isAdmin = this.authStore.isAdmin;
  
  /** Whether user has write permissions */
  readonly canWrite = this.authStore.canWrite;
  
  /** Whether user is read-only */
  readonly isReadOnly = this.authStore.isReadOnly;

  // 4. Initialization (runs on service creation)
  constructor() {
    // Initialize state from storage on service creation
    this.initializeFromStorage();
  }

  // 5. Private helper methods
  /**
   * Initializes authentication state from localStorage
   * Called automatically on service creation
   */
  private initializeFromStorage(): void {
    const user = this.tokenStorage.getUser();
    const hasValidToken = this.tokenStorage.hasValidToken();

    if (user && hasValidToken) {
      this.authStore.setUser(user);
    } else {
      // Token expired or invalid, clear storage
      this.tokenStorage.clear();
      this.authStore.clearUser();
    }
  }

  /**
   * Registers a new user
   */
  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.API_BASE}/register`, request);
  }

  /**
   * Authenticates a user and stores the JWT token
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_BASE}/login`, request).pipe(
      tap(response => {
        // Map LoginResponse to User object
        const user: User = {
          id: response.userId,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName,
          role: response.role
        };

        // Store token and user data
        this.tokenStorage.saveToken(response.token);
        this.tokenStorage.saveUser(user);

        // Update store state
        this.authStore.setUser(user);
      })
    );
  }

  /**
   * Logs out the current user and clears authentication state
   */
  logout(): void {
    // Clear storage
    this.tokenStorage.clear();

    // Update store state
    this.authStore.clearUser();

    // Redirect to login page
    this.router.navigate(['/login']);
  }

  /**
   * Checks if the current user has the specified role
   */
  hasRole(role: 'Admin' | 'User' | 'ReadOnly'): boolean {
    return this.authStore.hasRole(role);
  }

  /**
   * Checks if the current user has any of the specified roles
   */
  hasAnyRole(...roles: ('Admin' | 'User' | 'ReadOnly')[]): boolean {
    return this.authStore.hasAnyRole(...roles);
  }

  /**
   * Gets the current user's ID
   */
  getUserId(): string | null {
    return this.authStore.getUserId();
  }

  /**
   * Gets the current user's full name
   */
  getUserFullName(): string | null {
    return this.authStore.userFullName();
  }
}
