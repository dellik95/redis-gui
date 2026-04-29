import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';

/**
 * Guard that checks if the user is authenticated
 * Redirects to /login if not authenticated
 */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Redirect to login page
  return router.createUrlTree(['/login']);
};

/**
 * Guard that checks if the user has Admin role
 * Redirects to home page if not an admin
 */
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.hasRole('Admin')) {
    return true;
  }

  // Redirect to home page
  return router.createUrlTree(['/home']);
};

/**
 * Guard that checks if the user has write access (Admin or User role)
 * Excludes ReadOnly users
 * Redirects to home page if user doesn't have write access
 */
export const writeAccessGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.canWrite()) {
    return true;
  }

  // Redirect to home page
  return router.createUrlTree(['/home']);
};

/**
 * Guard that redirects authenticated users away from public pages
 * Useful for login/register pages - if already logged in, redirect to home
 */
export const guestOnlyGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  // Already logged in, redirect to home
  return router.createUrlTree(['/home']);
};
