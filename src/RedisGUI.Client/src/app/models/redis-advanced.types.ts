// Pagination types
export interface PagedRedisValuesResponse {
  values: RedisKeyValuePair[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface RedisKeyValuePair {
  key: string;
  value: string;
  type: import('./redis-keys.types').RedisWireDataType;
  timeToLive: number | null;
}

// Health monitoring types
export enum ConnectionHealthStatus {
  Unknown = 0,
  Healthy = 1,
  Degraded = 2,
  Unhealthy = 3
}

export interface GetConnectionHealthResponse {
  connectionId: string;
  status: string;
  lastChecked: Date;
  responseTimeMs: number;
  errorMessage?: string;
  consecutiveFailures: number;
  failureStreak?: number;
  checkedAt?: Date;
  serverMode?: 'Unknown' | 'Standalone' | 'Cluster' | 'Sentinel' | undefined;
}
