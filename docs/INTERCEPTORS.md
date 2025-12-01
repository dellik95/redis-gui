# HTTP Interceptors Architecture

## Overview
Implemented a comprehensive HTTP interceptor system to handle errors, retries, and loading states globally across the application.

## Interceptors

### 1. RetryInterceptor (NEW)
**File:** `src/shared/interceptors/retry.interceptor.ts`

**Purpose:** Automatically retries failed HTTP requests with exponential backoff strategy.

**Features:**
- Only retries idempotent methods: GET, PUT, DELETE
- Excludes POST to prevent duplicate operations
- Exponential backoff: 1s, 2s, 4s
- Maximum 2 retries
- Skips retry for client errors (4xx except 408, 429)
- Retries server errors (5xx) and network issues

**Configuration:**
```typescript
MAX_RETRIES = 2
RETRY_DELAY_MS = 1000
RETRYABLE_METHODS = ['GET', 'PUT', 'DELETE']
NON_RETRYABLE_STATUS_CODES = [400, 401, 403, 404, 422]
```

### 2. ErrorInterceptor (ENHANCED)
**File:** `src/shared/interceptors/error.interceptor.ts`

**Purpose:** Global error handling with user-friendly notifications.

**Enhancements:**
- Comprehensive error categorization for all HTTP status codes
- Smart message extraction from various server response formats
- Detailed error logging with grouped console output
- Material snackbar notifications for user feedback
- Handles JSON, string, and blob error responses

**Error Messages by Status Code:**
- `0`: Cannot connect to server
- `400`: Invalid request
- `401`: Session expired
- `403`: Permission denied
- `404`: Resource not found
- `408`: Request timeout
- `409`: Conflict
- `422`: Validation failed
- `429`: Too many requests
- `500`: Internal server error
- `502`: Bad gateway
- `503`: Service unavailable
- `504`: Gateway timeout

### 3. LoaderInterceptor (UPDATED)
**File:** `src/shared/interceptors/loader.interceptor.ts`

**Purpose:** Shows/hides loading indicator during HTTP requests.

**Changes:**
- Added `@Injectable()` decorator
- Implements `HttpInterceptor` interface
- Proper TypeScript compliance

## Execution Order
Interceptors are registered in this order in `app.config.ts`:
1. **RetryInterceptor** - Attempts to retry failed requests first
2. **ErrorInterceptor** - Catches and formats errors
3. **LoaderInterceptor** - Manages loading state

## Service Cleanup

### Removed Duplicate Code
All HTTP services were cleaned to remove duplicate error handling:

**Files Updated:**
- `redis-keys.service.ts`
- `storage-connections.service.ts`
- `redis-operations.service.ts`

**Removed:**
- Individual `handleError()` methods (45+ lines per service)
- Manual `retry()` operators with custom backoff
- Manual `catchError()` pipes
- Duplicate error message mapping

**Benefits:**
- ✅ Reduced code duplication by ~150 lines
- ✅ Consistent error handling across all services
- ✅ Centralized retry logic
- ✅ Easier maintenance
- ✅ Cleaner service code
- ✅ Single source of truth for error messages

## Configuration

### app.config.ts
```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: RetryInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoaderInterceptor,
      multi: true
    }
  ]
};
```

## Usage

Services now have clean HTTP calls without error handling:

### Before:
```typescript
return this.httpClient.get<Data>(url).pipe(
  retry({
    count: 2,
    delay: (error, retryCount) => {
      const delayMs = Math.pow(2, retryCount) * 1000;
      return timer(delayMs);
    }
  }),
  catchError(this.handleError)
);
```

### After:
```typescript
return this.httpClient.get<Data>(url);
```

## Benefits

1. **DRY Principle**: No duplicate error handling code
2. **Consistency**: All HTTP errors handled the same way
3. **Maintainability**: Single place to update error messages
4. **Automatic Retry**: Network issues handled automatically
5. **User Experience**: Clear, friendly error messages
6. **Debugging**: Detailed console logging
7. **Performance**: Smart retry strategy prevents server overload
8. **Type Safety**: Full TypeScript support

## Testing

To test the interceptors:
1. **Retry**: Disconnect network and make a request - should retry 2 times
2. **Error**: Send invalid data - should show snackbar with friendly message
3. **Loading**: Make any request - should show loading indicator
4. **Order**: All three should work together seamlessly

## Future Enhancements

Potential improvements:
- Add logging service integration for error tracking
- Implement request caching interceptor
- Add authentication token interceptor
- Create request/response transformation interceptor
- Add request throttling/debouncing
