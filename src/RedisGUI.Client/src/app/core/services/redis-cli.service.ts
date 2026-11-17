import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CliExecutionResponse {
  output: string;
  success: boolean;
}

export interface KeyspaceInfoResponse {
  databases: Record<number, number>;
}

export interface KeyMemoryResponse {
  key: string;
  memoryBytes: number;
}

export interface LoadTestRequest {
  operationsCount?: number | undefined;
  concurrency?: number | undefined;
  operationType?: 'mixed' | 'write' | 'read' | undefined;
  payloadSizeBytes?: number | undefined;
  database?: number | undefined;
  keyPrefix?: string | undefined;
  durationSeconds?: number | undefined;
}

export interface LoadTestProgress {
  elapsedSeconds: number;
  totalDurationSeconds: number;
  completedOperations: number;
  totalOperations: number;
  currentOpsPerSecond: number;
  overallOpsPerSecond: number;
  avgLatencyMs: number;
  successRatePercent: number;
  inProgress: boolean;
}

export interface LoadTestResult {
  totalOperations: number;
  successfulOperations: number;
  failedOperations: number;
  totalDurationMs: number;
  opsPerSecond: number;
  avgLatencyMs: number;
  minLatencyMs: number;
  maxLatencyMs: number;
  p50LatencyMs: number;
  p90LatencyMs: number;
  p95LatencyMs: number;
  p99LatencyMs: number;
  cleanedKeysCount: number;
  operationType: string;
  concurrency: number;
  executedAt: string;
}

export interface SeedSampleDataOptions {
  keyCount?: number | undefined;
  targetDatabases?: number[] | undefined;
  distributeRandomlyAcrossDatabases?: boolean | undefined;
  randomDbCount?: number | undefined;
  keyComplexity?: 'simple' | 'hierarchical' | 'uuid' | 'hashtag' | undefined;
  payloadComplexity?: 'simple' | 'json' | 'complex' | undefined;
  dataTypes?: string[] | undefined;
  ttlStrategy?: 'mixed' | 'persistent' | 'expiring' | undefined;
  keyPrefix?: string | undefined;
}

export interface SeedDataResult {
  totalKeysCreated: number;
  targetDatabases: number[];
  keyCountPerType: Record<string, number>;
  summary: string;
  cancelled: boolean;
  mayHaveUnconfirmedChanges: boolean;
  error: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class RedisCliService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/storage';

  /**
   * Executes a raw Redis CLI command
   */
  executeCommand(
    connectionId: string,
    command: string,
    database?: number
  ): Observable<CliExecutionResponse> {
    return this.http.post<CliExecutionResponse>(`${this.baseUrl}/${connectionId}/cli`, {
      command,
      database
    });
  }

  /**
   * Retrieves keyspace information across all databases (DB 0-15)
   */
  getKeyspaceInfo(connectionId: string): Observable<KeyspaceInfoResponse> {
    return this.http.get<KeyspaceInfoResponse>(`${this.baseUrl}/${connectionId}/keyspace`);
  }

  /**
   * Retrieves memory usage of a specific Redis key
   */
  getKeyMemoryUsage(connectionId: string, key: string): Observable<KeyMemoryResponse> {
    return this.http.get<KeyMemoryResponse>(
      `${this.baseUrl}/${connectionId}/keys/${encodeURIComponent(key)}/memory`
    );
  }

}
