import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenStorageService } from '../services/auth/token-storage.service';

/**
 * Functional HTTP Interceptor that adds JWT token to outgoing requests
 * Automatically attaches Authorization header with Bearer token if available
 * Compatible with Angular 21 zoneless mode and signal-based token storage
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);

  // Use signal-based token retrieval (reactive)
  const token = tokenStorage.token();

  // Skip adding token if it's expired or doesn't exist (computed signal)
  if (!token || tokenStorage.isTokenExpired()) {
    return next(req);
  }

  // Clone request and add Authorization header
  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authReq);
};
