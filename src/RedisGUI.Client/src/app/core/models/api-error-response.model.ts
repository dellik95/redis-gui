/**
 * Standardized API error response matching backend structure
 */
export interface ApiErrorResponse {
  /** Error code for programmatic handling */
  code: string;
  
  /** Human-readable error message */
  message: string;
  
  /** HTTP status code */
  statusCode: number;
  
  /** Timestamp when the error occurred (ISO string) */
  timestamp: string;
  
  /** Request path where the error occurred */
  path?: string;
  
  /** Additional error details (for development/debugging) */
  details?: Record<string, unknown>;
}

/**
 * Type guard to check if an error response is an ApiErrorResponse
 * 
 * @param error - The error object to check
 * @returns True if the error is an ApiErrorResponse
 */
export function isApiErrorResponse(error: unknown): error is ApiErrorResponse {
  return (
    error !== null &&
    error !== undefined &&
    typeof error === 'object' &&
    'code' in error &&
    'message' in error &&
    'statusCode' in error &&
    typeof (error as Record<string, unknown>)['code'] === 'string' &&
    typeof (error as Record<string, unknown>)['message'] === 'string' &&
    typeof (error as Record<string, unknown>)['statusCode'] === 'number'
  );
}
