import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';
import { retry, mergeMap } from 'rxjs/operators';

/**
 * HTTP Interceptor that automatically retries failed requests with exponential backoff
 * Applies to GET requests and certain idempotent operations
 */
@Injectable()
export class RetryInterceptor implements HttpInterceptor {
  private readonly MAX_RETRIES = 2;
  private readonly RETRY_DELAY_MS = 1000;
  private readonly RETRYABLE_METHODS = ['GET', 'PUT', 'DELETE'];
  private readonly NON_RETRYABLE_STATUS_CODES = [400, 401, 403, 404, 422];

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Only retry certain HTTP methods (idempotent operations)
    if (!this.RETRYABLE_METHODS.includes(req.method)) {
      return next.handle(req);
    }

    return next.handle(req).pipe(
      retry({
        count: this.MAX_RETRIES,
        delay: (error: HttpErrorResponse, retryCount: number) => {
          // Don't retry client errors (4xx except 408 Request Timeout and 429 Too Many Requests)
          if (
            this.NON_RETRYABLE_STATUS_CODES.includes(error.status) ||
            (error.status >= 400 && error.status < 500 && error.status !== 408 && error.status !== 429)
          ) {
            return throwError(() => error);
          }

          // Exponential backoff: 1s, 2s, 4s...
          const delayMs = this.RETRY_DELAY_MS * Math.pow(2, retryCount - 1);
          
          console.log(
            `Retrying request (${retryCount}/${this.MAX_RETRIES}): ${req.method} ${req.url}`,
            `Delay: ${delayMs}ms`
          );

          return timer(delayMs);
        }
      })
    );
  }
}
