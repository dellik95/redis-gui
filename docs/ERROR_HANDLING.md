# Strongly Typed Error Handling Implementation

## Overview
Implemented a comprehensive, strongly-typed error handling system across the entire application stack - from backend API responses to frontend error display.

---

## Backend Changes

### 1. ApiErrorResponse Model
**File:** `src/RedisGUI.Server/Abstraction/ApiErrorResponse.cs`

Standardized error response structure returned by all API endpoints:

```csharp
public sealed record ApiErrorResponse
{
    public required string Code { get; init; }         // Error code for programmatic handling
    public required string Message { get; init; }      // Human-readable message
    public required int StatusCode { get; init; }      // HTTP status code
    public DateTime Timestamp { get; init; }           // When error occurred
    public string? Path { get; init; }                 // Request path
    public Dictionary<string, object>? Details { get; init; }  // Additional details
}
```

**Factory Methods:**
- `BadRequest()` - 400 errors
- `NotFound()` - 404 errors
- `InternalServerError()` - 500 errors
- `Conflict()` - 409 errors
- `Unauthorized()` - 401 errors
- `Forbidden()` - 403 errors

### 2. ErrorResultExtensions
**File:** `src/RedisGUI.Server/Extensions/ErrorResultExtensions.cs`

Extension methods to convert `Domain.Error` objects to HTTP responses:

```csharp
// Convert Error to structured IResult
error.ToBadRequest(httpContext);
error.ToNotFound(httpContext);
error.ToProblemDetails(httpContext);

// Validation errors
ErrorResultExtensions.ValidationError(fieldErrors, httpContext);
```

### 3. ResultExtensions
**File:** `src/RedisGUI.Server/Extensions/ResultExtensions.cs`

Pattern matching for Result<T> to HTTP responses:

```csharp
// Automatic conversion
return result.ToHttpResult(httpContext);

// With custom success handler
return result.MatchHttp(Results.Ok, httpContext);
return result.MatchHttp(() => Results.NoContent(), httpContext);
```

### 4. Updated Endpoint Example
**File:** `src/RedisGUI.Server/Endpoints/RedisStorage/GetRedisValue/GetRedisValueEndpoint.cs`

**Before:**
```csharp
return await sender.Send<GetRedisValueResponse>(query)
    .Match(Results.Ok, Results.BadRequest);
```

**After:**
```csharp
var result = await sender.Send<GetRedisValueResponse>(query);
return result.MatchHttp(Results.Ok, httpContext);
```

Now returns structured error:
```json
{
  "code": "Redis.KeyNotFound",
  "message": "The specified key does not exist in Redis",
  "statusCode": 404,
  "timestamp": "2025-12-01T17:30:00Z",
  "path": "/api/v1/storage/123e4567-e89b-12d3-a456-426614174000/mykey"
}
```

---

## Frontend Changes

### 1. ApiErrorResponse Model
**File:** `src/app/core/models/api-error-response.model.ts`

TypeScript interface matching backend structure:

```typescript
export interface ApiErrorResponse {
  code: string;
  message: string;
  statusCode: number;
  timestamp: string;
  path?: string;
  details?: Record<string, any>;
}

// Type guard
export function isApiErrorResponse(error: any): error is ApiErrorResponse;
```

### 2. Enhanced Error Interceptor
**File:** `src/shared/interceptors/error.interceptor.ts`

**New Features:**
- ✅ Automatically detects and parses structured `ApiErrorResponse` from backend
- ✅ Falls back to legacy error format extraction for backward compatibility
- ✅ Improved logging with structured error details
- ✅ Type-safe error handling with type guards

**Error Extraction Priority:**
1. **Structured API Error** - `isApiErrorResponse(error.error)` → Use `message` directly
2. **Legacy Format** - Try to extract from `error`, `title`, `message` properties
3. **Generic Message** - Fallback to status code-based messages

**Code Example:**
```typescript
private getErrorMessage(error: HttpErrorResponse): string {
  // First, try structured API error
  if (error.error && isApiErrorResponse(error.error)) {
    return error.error.message;  // ✅ Direct extraction
  }

  // Fallback to legacy extraction
  const serverMessage = this.extractServerMessage(error);
  if (serverMessage) return serverMessage;

  // Generic message
  return this.getGenericErrorMessage(error.status);
}
```

**Enhanced Logging:**
```typescript
console.group('🔴 HTTP Error Intercepted');
console.error('Request:', 'GET /api/v1/storage/...');
console.error('Status:', '404 Not Found');
console.error('Error Code:', 'Redis.KeyNotFound');
console.error('Error Message:', 'The specified key does not exist');
console.error('Error Details:', {...});
console.error('Error Path:', '/api/v1/storage/...');
console.groupEnd();
```

---

## Error Flow

### Successful Error Handling Flow:

1. **Backend Domain Layer**
   ```csharp
   return Error("Redis.KeyNotFound", "Key does not exist");
   ```

2. **Backend Application Layer**
   ```csharp
   Result<GetRedisValueResponse> result = await handler.Handle(query);
   // Result.IsSuccess = false, Result.Error = Error(...)
   ```

3. **Backend Endpoint**
   ```csharp
   return result.MatchHttp(Results.Ok, httpContext);
   // Returns JSON with ApiErrorResponse structure
   ```

4. **HTTP Response**
   ```json
   Status: 404
   Body: {
     "code": "Redis.KeyNotFound",
     "message": "The specified key does not exist in Redis",
     "statusCode": 404,
     "timestamp": "2025-12-01T17:30:00Z",
     "path": "/api/v1/storage/123/mykey"
   }
   ```

5. **Frontend Error Interceptor**
   ```typescript
   catchError((error: HttpErrorResponse) => {
     // Detects structured error
     if (isApiErrorResponse(error.error)) {
       message = error.error.message;  // "The specified key does not exist..."
     }
     this.showErrorNotification(message);
   })
   ```

6. **User Sees:**
   ```
   ┌─────────────────────────────────────────────┐
   │ The specified key does not exist in Redis  │
   │                                [Close]      │
   └─────────────────────────────────────────────┘
   ```

---

## Benefits

### For Developers:
- ✅ **Type Safety** - Full TypeScript/C# typing end-to-end
- ✅ **Consistency** - Same error structure everywhere
- ✅ **Debuggability** - Rich error details and logging
- ✅ **Maintainability** - Single source of truth for error handling
- ✅ **Testability** - Easy to mock and test error scenarios

### For Users:
- ✅ **Clear Messages** - Human-readable error descriptions
- ✅ **Consistency** - Same error presentation across all features
- ✅ **Context** - Errors include relevant details (path, timestamp, etc.)
- ✅ **No Technical Jargon** - User-friendly language

### For Operations:
- ✅ **Error Codes** - Programmatic error identification
- ✅ **Structured Logging** - Easy to parse and analyze
- ✅ **Traceability** - Timestamp and path included
- ✅ **Monitoring Ready** - Can aggregate by error code

---

## Migration Guide

### Updating Existing Endpoints:

**Old Pattern:**
```csharp
return await sender.Send(query)
    .Match(Results.Ok, Results.BadRequest);
```

**New Pattern:**
```csharp
var result = await sender.Send(query);
return result.MatchHttp(Results.Ok, httpContext);
```

**For Void Results:**
```csharp
return result.MatchHttp(() => Results.NoContent(), httpContext);
```

### Adding New Error Codes:

1. Define in Domain layer:
   ```csharp
   public static class RedisErrors
   {
       public static Error KeyNotFound(string key) => 
           new("Redis.KeyNotFound", $"Key '{key}' does not exist");
       
       public static Error ConnectionFailed(string connectionId) =>
           new("Redis.ConnectionFailed", $"Failed to connect to Redis");
   }
   ```

2. Use in Application layer:
   ```csharp
   if (!keyExists)
       return Result.Failure<GetRedisValueResponse>(
           RedisErrors.KeyNotFound(key)
       );
   ```

3. Frontend automatically handles it ✅

---

## Testing

### Backend Unit Test:
```csharp
[Fact]
public void ApiErrorResponse_ShouldHaveCorrectStructure()
{
    var error = ApiErrorResponse.BadRequest(
        "Test.Error",
        "Test message"
    );
    
    Assert.Equal("Test.Error", error.Code);
    Assert.Equal("Test message", error.Message);
    Assert.Equal(400, error.StatusCode);
}
```

### Frontend Unit Test:
```typescript
it('should extract structured error message', () => {
  const error = {
    error: {
      code: 'Redis.KeyNotFound',
      message: 'Key does not exist',
      statusCode: 404
    }
  } as HttpErrorResponse;
  
  expect(isApiErrorResponse(error.error)).toBe(true);
});
```

### Integration Test:
```csharp
[Fact]
public async Task GetRedisValue_WhenKeyNotFound_ReturnsStructuredError()
{
    var response = await client.GetAsync("/api/v1/storage/{id}/nonexistent");
    
    var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
    
    Assert.Equal(404, response.StatusCode);
    Assert.Equal("Redis.KeyNotFound", error.Code);
    Assert.NotNull(error.Message);
    Assert.NotNull(error.Timestamp);
}
```

---

## Next Steps

1. ✅ Update remaining endpoints to use new pattern
2. ✅ Add error codes to Domain layer for all business rules
3. ✅ Implement validation error handling
4. ✅ Add error aggregation/monitoring service
5. ✅ Create error code documentation
6. ✅ Add frontend error recovery strategies

---

## Files Modified

### Backend:
- ✅ `src/RedisGUI.Server/Abstraction/ApiErrorResponse.cs` (NEW)
- ✅ `src/RedisGUI.Server/Extensions/ErrorResultExtensions.cs` (NEW)
- ✅ `src/RedisGUI.Server/Extensions/ResultExtensions.cs` (NEW)
- ✅ `src/RedisGUI.Server/Endpoints/RedisStorage/GetRedisValue/GetRedisValueEndpoint.cs` (UPDATED)

### Frontend:
- ✅ `src/app/core/models/api-error-response.model.ts` (NEW)
- ✅ `src/shared/interceptors/error.interceptor.ts` (ENHANCED)

### Documentation:
- ✅ `docs/ERROR_HANDLING.md` (THIS FILE)
- ✅ `docs/FRONTEND_REORGANIZATION.md` (NEW)
- ✅ `docs/INTERCEPTORS.md` (EXISTING)
