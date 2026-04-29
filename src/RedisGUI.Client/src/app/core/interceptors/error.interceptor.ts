import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { isApiErrorResponse } from '../models/api-error-response.model';
import { AuthService } from '../services/auth/auth.service';

/**
 * Functional HTTP Error Interceptor
 * Catches all HTTP errors, extracts structured error responses from backend,
 * displays user-friendly messages via snackbar, and handles auto-logout on 401
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  const authService = inject(AuthService);
  const durationInSeconds = 5;

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const errorMessage = getErrorMessage(error);
      
      // Log error for debugging
      logError(error, req);
      
      // Show user-friendly notification
      showErrorNotification(snackBar, errorMessage, durationInSeconds);
      
      // Handle 401 Unauthorized - auto logout (clears token + state and redirects)
      if (error.status === 401) {
        authService.logout();
      }
      
      return throwError(() => error);
    })
  );
};

/**
 * Extract error message from HTTP error response
 */
function getErrorMessage(error: HttpErrorResponse): string {
  // First, try to extract structured API error response from backend
  if (error.error && isApiErrorResponse(error.error)) {
    return error.error.message;
  }

  // Handle network/client errors
  if (error.error instanceof ErrorEvent) {
    return `Network Error: ${error.error.message}`;
  }

  // Try to extract any message from error body
  const serverMessage = extractServerMessage(error);
  if (serverMessage) {
    return serverMessage;
  }

  // Fallback to generic messages based on status code
  return getGenericErrorMessage(error.status);
}

/**
 * Get generic error message based on HTTP status code
 */
function getGenericErrorMessage(status: number): string {
  switch (status) {
    case 0:
      return 'Cannot connect to server. Please check your internet connection.';
    case 400:
      return 'Invalid request. Please check your input and try again.';
    case 401:
      return 'Your session has expired. Please log in again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested resource was not found.';
    case 408:
      return 'Request timeout. Please try again.';
    case 409:
      return 'Conflict: This resource already exists or is in use.';
    case 422:
      return 'Validation failed. Please check your input.';
    case 429:
      return 'Too many requests. Please slow down and try again later.';
    case 500:
      return 'Internal server error. Our team has been notified.';
    case 502:
      return 'Bad gateway. The server is temporarily unavailable.';
    case 503:
      return 'Service unavailable. Please try again in a few moments.';
    case 504:
      return 'Gateway timeout. The server took too long to respond.';
    default:
      return `An unexpected error occurred (${status})`;
  }
}

/**
 * Extract error message from server response (legacy/unstructured format)
 * Handles various error response formats for backward compatibility
 */
function extractServerMessage(error: HttpErrorResponse): string | null {
  if (!error.error) {
    return null;
  }

  // Handle string error
  if (typeof error.error === 'string') {
    try {
      const parsed = JSON.parse(error.error);
      return parsed.message || parsed.error || parsed.title || null;
    } catch {
      return error.error;
    }
  }

  // Handle object error with various message properties
  if (typeof error.error === 'object') {
    return error.error.message || 
           error.error.error || 
           error.error.title || 
           error.error.detail ||
           error.error.errorMessage ||
           null;
  }

  return null;
}

/**
 * Log error details for debugging
 */
function logError(error: HttpErrorResponse, request: { method: string; url: string }): void {
  // Extract structured error if available
  const structuredError = isApiErrorResponse(error.error) ? error.error : null;

  console.group('🔴 HTTP Error Intercepted');
  console.error('Request:', `${request.method} ${request.url}`);
  console.error('Status:', `${error.status} ${error.statusText}`);
  
  if (structuredError) {
    console.error('Error Code:', structuredError.code);
    console.error('Error Message:', structuredError.message);
    if (structuredError.details) {
      console.error('Error Details:', structuredError.details);
    }
    if (structuredError.path) {
      console.error('Error Path:', structuredError.path);
    }
  } else {
    console.error('Message:', getErrorMessage(error));
    console.error('Full Error:', error);
  }
  
  console.groupEnd();

  // You can send this to a logging service in production
  // this.loggingService.logError({...});
}

/**
 * Display error notification to user
 */
function showErrorNotification(snackBar: MatSnackBar, message: string, durationInSeconds: number): void {
  snackBar.open(message, 'Close', {
    duration: durationInSeconds * 1000,
    horizontalPosition: 'end',
    verticalPosition: 'top',
    panelClass: ['error-snackbar']
  });
}

