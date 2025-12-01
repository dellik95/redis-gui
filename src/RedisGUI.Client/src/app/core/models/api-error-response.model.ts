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
  details?: Record<string, any>;
}

/**
 * Type guard to check if an error response is an ApiErrorResponse
 */
export function isApiErrorResponse(error: any): error is ApiErrorResponse {
  return (
    error &&
    typeof error === 'object' &&
    'code' in error &&
    'message' in error &&
    'statusCode' in error &&
    typeof error.code === 'string' &&
    typeof error.message === 'string' &&
    typeof error.statusCode === 'number'
  );
}
