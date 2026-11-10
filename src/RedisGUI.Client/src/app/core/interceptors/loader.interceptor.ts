import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LoadingStore } from '../stores/loading.store';

/**
 * Functional HTTP Loader Interceptor
 * Shows/hides loading indicator during HTTP requests
 * Uses LoadingStore for centralized state management
 * 
 * Excludes health check requests from triggering global loading indicator
 */
export const loaderInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingStore = inject(LoadingStore);
  
  // Skip loading indicator for health check requests
  // These are periodic background requests that shouldn't show global loading
  if (req.url.includes('/health') || req.url.includes('/health/')) {
    return next(req);
  }
  
  return loadingStore.showLoaderUntilCompleted(next(req));
};

