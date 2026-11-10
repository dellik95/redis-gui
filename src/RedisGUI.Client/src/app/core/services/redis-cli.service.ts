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
	executeCommand(connectionId: string, command: string, database?: number): Observable<CliExecutionResponse> {
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
		return this.http.get<KeyMemoryResponse>(`${this.baseUrl}/${connectionId}/keys/${encodeURIComponent(key)}/memory`);
	}

	/**
	 * Executes synthetic Redis load test with automated cleanup (one-shot POST)
	 */
	runLoadTest(connectionId: string, request: LoadTestRequest): Observable<LoadTestResult> {
		return this.http.post<LoadTestResult>(`${this.baseUrl}/${connectionId}/load-test`, request);
	}

	/**
	 * Starts real-time streaming load test via Server-Sent Events (SSE)
	 */
	streamLoadTest(
		connectionId: string,
		request: LoadTestRequest,
		testId: string,
		onProgress: (progress: LoadTestProgress) => void,
		onComplete: (result: LoadTestResult) => void,
		onError: (error: string) => void
	): AbortController {
		const controller = new AbortController();
		const url = `${this.baseUrl}/${connectionId}/load-test/stream?testId=${encodeURIComponent(testId)}`;

		fetch(url, {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify(request),
			signal: controller.signal
		}).then(async (response) => {
			if (!response.ok) {
				const errText = await response.text();
				onError(errText || `Server returned ${response.status}`);
				return;
			}

			const reader = response.body?.getReader();
			if (!reader) {
				onError('ReadableStream not supported by browser');
				return;
			}

			const decoder = new TextDecoder();
			let buffer = '';

			while (true) {
				const { done, value } = await reader.read();
				if (done) break;

				buffer += decoder.decode(value, { stream: true });
				const events = buffer.split('\n\n');
				buffer = events.pop() ?? '';

				for (const eventBlock of events) {
					if (!eventBlock.trim()) continue;

					let currentEvent = 'message';
					let currentData = '';

					for (const line of eventBlock.split('\n')) {
						if (line.startsWith('event: ')) {
							currentEvent = line.slice(7).trim();
						} else if (line.startsWith('data: ')) {
							currentData = line.slice(6).trim();
						}
					}

					if (currentEvent === 'progress' && currentData) {
						try {
							onProgress(JSON.parse(currentData));
						} catch (e) {
							console.error('Error parsing progress SSE', e);
						}
					} else if (currentEvent === 'complete' && currentData) {
						try {
							onComplete(JSON.parse(currentData));
						} catch (e) {
							console.error('Error parsing complete SSE', e);
						}
					} else if (currentEvent === 'error' && currentData) {
						try {
							const parsed = JSON.parse(currentData);
							onError(parsed.error || currentData);
						} catch {
							onError(currentData);
						}
					}
				}
			}
		}).catch((err) => {
			if (err.name !== 'AbortError') {
				onError(err.message || 'Stream connection error');
			}
		});

		return controller;
	}

	/**
	 * Stops a running load test immediately
	 */
	stopLoadTest(connectionId: string, testId: string): Observable<{ message: string; stopped: boolean }> {
		return this.http.post<{ message: string; stopped: boolean }>(
			`${this.baseUrl}/${connectionId}/load-test/stop?testId=${encodeURIComponent(testId)}`,
			{}
		);
	}

	/**
	 * Seeds customizable sample data
	 */
	seedSampleData(connectionId: string, options?: SeedSampleDataOptions): Observable<SeedDataResult> {
		return this.http.post<SeedDataResult>(`${this.baseUrl}/${connectionId}/seed-sample-data`, options ?? {});
	}
}
