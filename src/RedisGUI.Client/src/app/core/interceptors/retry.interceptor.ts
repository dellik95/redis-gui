import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { retry, throwError, timer } from 'rxjs';

/**
 * Functional HTTP Retry Interceptor
 * Automatically retries failed requests with exponential backoff
 * Applies to GET, PUT, DELETE requests (idempotent operations)
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  const MAX_RETRIES = 2;
  const RETRY_DELAY_MS = 1000;
  const RETRYABLE_METHODS = ['GET', 'PUT', 'DELETE'];
  const NON_RETRYABLE_STATUS_CODES = [400, 401, 403, 404, 422];

  // Only retry certain HTTP methods (idempotent operations)
  if (!RETRYABLE_METHODS.includes(req.method)) {
    return next(req);
  }

  return next(req).pipe(
    retry({
      count: MAX_RETRIES,
      delay: (error: HttpErrorResponse, retryCount: number) => {
        // Don't retry client errors (4xx except 408 Request Timeout and 429 Too Many Requests)
        if (
          NON_RETRYABLE_STATUS_CODES.includes(error.status) ||
          (error.status >= 400 && error.status < 500 && error.status !== 408 && error.status !== 429)
        ) {
          return throwError(() => error);
        }

        // Exponential backoff: 1s, 2s, 4s...
        const delayMs = RETRY_DELAY_MS * Math.pow(2, retryCount - 1);
        
        // TODO: Integrate with a structured logger or telemetry system for diagnostics purposes in production environments.

        return timer(delayMs);
      }
    })
  );
};

