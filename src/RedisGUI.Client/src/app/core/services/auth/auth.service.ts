import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_ROUTES } from '../../constants/api-routes.constants';
import { TokenStorageService } from './token-storage.service';
import { ConnectionsStore } from '../../stores/connections.store';
import { AuthStore } from '../../stores/auth.store';
import {
  User,
  UserRole,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse
} from '../../models/auth';

/**
 * Service for handling user authentication
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // 1. Injected dependencies
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly connectionsStore = inject(ConnectionsStore);
  private readonly authStore = inject(AuthStore);

  // 3. Exposed store signals
  /** Current authenticated user */
  readonly currentUser = this.authStore.user;

  /** Whether user is authenticated */
  isAuthenticated(): boolean {
    return this.authStore.isAuthenticated() && this.tokenStorage.hasValidToken();
  }

  /** Whether user has admin role */
  readonly isAdmin = this.authStore.isAdmin;

  /** Whether user has write permissions */
  readonly canWrite = this.authStore.canWrite;

  /** Whether user is read-only */
  readonly isReadOnly = this.authStore.isReadOnly;

  // 4. Initialization (runs on service creation)
  constructor() {
    this.initializeFromStorage();
  }

  // 5. Private helper methods
  private initializeFromStorage(): void {
    const user = this.tokenStorage.getUser();
    const hasValidToken = this.tokenStorage.hasValidToken();

    if (user && hasValidToken) {
      this.authStore.setUser(user);
    } else {
      this.tokenStorage.clear();
      this.authStore.clearUser();
    }
  }

  /**
   * Registers a new user
   */
  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(API_ROUTES.AUTH.REGISTER, request);
  }

  /**
   * Authenticates a user and stores the JWT token
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(API_ROUTES.AUTH.LOGIN, request).pipe(
      tap(response => {
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
    this.connectionsStore.resetSession();
    this.tokenStorage.clear();
    this.authStore.clearUser();
    this.router.navigate(['/login']);
  }

  /**
   * Checks if the current user has the specified role
   */
  hasRole(role: UserRole): boolean {
    return this.authStore.hasRole(role);
  }

  /**
   * Checks if the current user has any of the specified roles
   */
  hasAnyRole(...roles: UserRole[]): boolean {
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
