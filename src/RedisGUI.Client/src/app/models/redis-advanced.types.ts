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
    type: string;
    timeToLive: number | null;
}

// Bulk delete types
export interface DeleteKeysByPatternResponse {
    deletedCount: number;
    pattern: string;
}

export interface DeleteMultipleKeysResponse {
    deletedCount: number;
    totalRequested: number;
    failedKeys: string[];
}

// Export/Import types
export enum ExportFormat {
    Json = 0,
    Csv = 1
}

export interface ExportRedisDataResponse {
    data: string;
    fileName: string;
    contentType: string;
}

export interface ImportRedisDataRequest {
    data: string;
    format: string;
    overwrite: boolean;
}

export interface ImportRedisDataResponse {
    importedCount: number;
    skippedCount: number;
    failedCount: number;
    failedKeys: string[];
}

// Pub/Sub types
export interface PublishMessageRequest {
    channel: string;
    message: string;
}

export interface SubscribeToChannelResponse {
    subscriptionId: string;
    channel: string;
    success: boolean;
}

// Transaction types
export interface RedisOperation {
    command: string;
    key: string;
    value?: string;
    arguments?: string[];
}

export interface ExecuteTransactionRequest {
    operations: RedisOperation[];
}

export interface ExecuteTransactionResponse {
    success: boolean;
    results: string[];
    operationsExecuted: number;
}

// Lua script types
export interface ExecuteLuaScriptRequest {
    script: string;
    keys?: string[];
    arguments?: string[];
}

export interface ExecuteLuaScriptResponse {
    result: string;
    success: boolean;
    scriptHash?: string;
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
}
